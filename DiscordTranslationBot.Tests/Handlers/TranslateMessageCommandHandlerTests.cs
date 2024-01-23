using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class TranslateMessageCommandHandlerTests : TestBase
{
    private const ulong BotUserId = 1UL;
    private readonly IMessageCommandInteraction _command;
    private readonly IMessage _message;
    private readonly TranslateMessageCommandHandler _sut;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;

    public TranslateMessageCommandHandlerTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        var client = Substitute.For<IDiscordClient>();
        client.CurrentUser.Id.Returns(BotUserId);

        _translationProviders = new List<TranslationProviderBase>
        {
            Substitute.For<TranslationProviderBase>(),
            Substitute.For<TranslationProviderBase>()
        };

        _sut = Substitute.ForPartsOf<TranslateMessageCommandHandler>(
            client,
            _translationProviders,
            CreateLogger<TranslateMessageCommandHandler>());

        _message = Substitute.For<IMessage>();
        _message.Author.Id.Returns(2UL);
        _message.Channel.Returns(Substitute.For<IMessageChannel>());

        var data = Substitute.For<IMessageCommandInteractionData>();
        data.Name.Returns(MessageCommandConstants.TranslateCommandName);
        data.Message.Returns(_message);

        _command = Substitute.For<IMessageCommandInteraction>();
        _command.Data.Returns(data);

        _sut.Configure().GetJumpUrl(_message).Returns(new Uri("http://localhost/test"));
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Success()
    {
        // Arrange
        _message.Content.Returns("text");
        _command.UserLocale.Returns("en-US");

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

        var notification = new MessageCommandExecutedNotification { Command = _command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _command.Received(1).DeferAsync(true, Arg.Any<RequestOptions>());

        await _command.Received(1)
            .FollowupAsync(
                embed: Arg.Is<Embed>(x => x.Title == "Translated Message"),
                options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_NotTranslateCommand_Returns()
    {
        // Arrange
        _command.Data.Name.Returns("not_the_translate_command");

        var notification = new MessageCommandExecutedNotification { Command = _command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = notification.Command.Data.Received(1).Name;
        _ = notification.Command.Data.Message.Author.DidNotReceive().Id;

        await notification.Command.DidNotReceive()
            .FollowupAsync(Arg.Any<string>(), ephemeral: Arg.Any<bool>(), options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Returns_WhenSanitizedMessageIsEmpty()
    {
        // Arrange
        _message.Content.Returns(string.Empty);

        var notification = new MessageCommandExecutedNotification { Command = _command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _command.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await _command.Received(1)
            .RespondAsync("No text to translate.", ephemeral: true, options: Arg.Any<RequestOptions>());

        _ = _translationProviders[0].DidNotReceive().SupportedLanguages;
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_UsesNextTranslationProvider_Success()
    {
        // Arrange
        _message.Content.Returns("text");
        _command.UserLocale.Returns("en");

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

        var notification = new MessageCommandExecutedNotification { Command = _command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(1).SupportedLanguages;
        _ = _translationProviders[1].Received(1).SupportedLanguages;

        await _command.Received(1)
            .FollowupAsync(
                embed: Arg.Is<Embed>(x => x.Title == "Translated Message"),
                options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        var notification = new MessageCommandExecutedNotification { Command = _command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _command.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await _command.Received(1)
            .RespondAsync(
                "Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Returns_IfNoProviderSupportsLocale()
    {
        // Arrange
        _message.Content.Returns("text");

        const string userLocale = "en-US";
        _command.UserLocale.Returns(userLocale);

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

        var notification = new MessageCommandExecutedNotification { Command = _command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _command.Received(1)
            .FollowupAsync(
                $"Your locale {userLocale} isn't supported for translation via this action.",
                options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Returns_WhenTranslatedTextIsSame()
    {
        // Arrange
        const string text = "text";

        _message.Content.Returns(text);
        _command.UserLocale.Returns("en-US");

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

        var notification = new MessageCommandExecutedNotification { Command = _command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _command.Received(1)
            .FollowupAsync(
                "The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
                options: Arg.Any<RequestOptions>());
    }
}
