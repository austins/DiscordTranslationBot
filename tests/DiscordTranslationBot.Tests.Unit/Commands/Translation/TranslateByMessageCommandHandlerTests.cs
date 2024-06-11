using Discord;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using Mediator;
using IMessage = Discord.IMessage;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

public sealed class TranslateByMessageCommandHandlerTests
{
    private const ulong BotUserId = 1UL;
    private readonly IMediator _mediator;
    private readonly IMessage _message;
    private readonly IMessageCommandInteraction _messageCommand;
    private readonly TranslateByMessageCommandHandler _sut;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;

    public TranslateByMessageCommandHandlerTests()
    {
        var client = Substitute.For<IDiscordClient>();
        client.CurrentUser.Id.Returns(BotUserId);

        _translationProviders = [Substitute.For<TranslationProviderBase>(), Substitute.For<TranslationProviderBase>()];

        _mediator = Substitute.For<IMediator>();

        _sut = Substitute.ForPartsOf<TranslateByMessageCommandHandler>(
            client,
            _translationProviders,
            _mediator,
            new LoggerFake<TranslateByMessageCommandHandler>());

        _message = Substitute.For<IMessage>();
        _message.Author.Id.Returns(2UL);
        _message.Channel.Returns(Substitute.For<IMessageChannel>());

        var data = Substitute.For<IMessageCommandInteractionData>();
        data.Name.Returns(MessageCommandConstants.Translate.CommandName);
        data.Message.Returns(_message);

        _messageCommand = Substitute.For<IMessageCommandInteraction>();
        _messageCommand.Data.Returns(data);

        _sut.Configure().GetJumpUrl(_message).Returns(new Uri("http://localhost/test"));
    }

    [Fact]
    public async Task Handle_TranslateByMessageCommand_Success()
    {
        // Arrange
        _message.Content.Returns("text");
        _messageCommand.UserLocale.Returns("en-US");

        var supportedLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        _translationProviders[0].SupportedLanguages.Returns(new HashSet<SupportedLanguage> { supportedLanguage });

        _translationProviders[0]
            .TranslateAsync(Arg.Any<SupportedLanguage>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = "fr",
                    DetectedLanguageName = "French",
                    TargetLanguageCode = supportedLanguage.LangCode,
                    TargetLanguageName = supportedLanguage.Name,
                    TranslatedText = "translated text"
                });

        var command = new TranslateByMessageCommand { MessageCommand = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _messageCommand.Received(1).DeferAsync(true, Arg.Any<RequestOptions>());

        await _messageCommand
            .Received(1)
            .FollowupAsync(
                embed: Arg.Is<Embed>(x => x.Title == "Translated Message"),
                options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedEvent_NotTranslateCommand_Returns()
    {
        // Arrange
        _messageCommand.Data.Name.Returns("not_the_translate_command");

        var notification = new MessageCommandExecutedEvent { MessageCommand = _messageCommand };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = notification.MessageCommand.Data.Received(1).Name;

        await _mediator.DidNotReceive().Send(Arg.Any<TranslateByMessageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByMessageCommand_Returns_WhenSanitizedMessageIsEmpty()
    {
        // Arrange
        _message.Content.Returns(string.Empty);

        var command = new TranslateByMessageCommand { MessageCommand = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _messageCommand.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await _messageCommand
            .Received(1)
            .RespondAsync("No text to translate.", ephemeral: true, options: Arg.Any<RequestOptions>());

        _ = _translationProviders[0].DidNotReceive().SupportedLanguages;
    }

    [Fact]
    public async Task Handle_TranslateByMessageCommand_UsesNextTranslationProvider_Success()
    {
        // Arrange
        _message.Content.Returns("text");
        _messageCommand.UserLocale.Returns("en");

        var supportedLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        _translationProviders[0].SupportedLanguages.Returns(new HashSet<SupportedLanguage>());

        _translationProviders[0]
            .TranslateAsync(Arg.Any<SupportedLanguage>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("test"));

        _translationProviders[1].SupportedLanguages.Returns(new HashSet<SupportedLanguage> { supportedLanguage });

        _translationProviders[1]
            .TranslateAsync(Arg.Any<SupportedLanguage>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = "fr",
                    DetectedLanguageName = "French",
                    TargetLanguageCode = supportedLanguage.LangCode,
                    TargetLanguageName = supportedLanguage.Name,
                    TranslatedText = "translated text"
                });

        var command = new TranslateByMessageCommand { MessageCommand = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(1).SupportedLanguages;
        _ = _translationProviders[1].Received(1).SupportedLanguages;

        await _messageCommand
            .Received(1)
            .FollowupAsync(
                embed: Arg.Is<Embed>(x => x.Title == "Translated Message"),
                options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_TranslateByMessageCommand_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        var command = new TranslateByMessageCommand { MessageCommand = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _messageCommand.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await _messageCommand
            .Received(1)
            .RespondAsync(
                "Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_TranslateByMessageCommand_Returns_IfNoProviderSupportsLocale()
    {
        // Arrange
        _message.Content.Returns("text");

        const string userLocale = "en-US";
        _messageCommand.UserLocale.Returns(userLocale);

        var supportedLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        foreach (var translationProvider in _translationProviders)
        {
            translationProvider.SupportedLanguages.Returns(new HashSet<SupportedLanguage> { supportedLanguage });

            translationProvider
                .TranslateAsync(Arg.Any<SupportedLanguage>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("test"));
        }

        var command = new TranslateByMessageCommand { MessageCommand = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _messageCommand
            .Received(1)
            .FollowupAsync(
                $"Your locale {userLocale} isn't supported for translation via this action.",
                options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_TranslateByMessageCommand_Returns_WhenTranslatedTextIsSame()
    {
        // Arrange
        const string text = "text";

        _message.Content.Returns(text);
        _messageCommand.UserLocale.Returns("en-US");

        var supportedLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        _translationProviders[0].SupportedLanguages.Returns(new HashSet<SupportedLanguage> { supportedLanguage });

        _translationProviders[0]
            .TranslateAsync(Arg.Any<SupportedLanguage>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = "fr",
                    DetectedLanguageName = "French",
                    TargetLanguageCode = supportedLanguage.LangCode,
                    TargetLanguageName = supportedLanguage.Name,
                    TranslatedText = text
                });

        var command = new TranslateByMessageCommand { MessageCommand = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _messageCommand
            .Received(1)
            .FollowupAsync(
                "The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
                options: Arg.Any<RequestOptions>());
    }
}
