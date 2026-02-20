using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Notifications.Handlers;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;
using IMessage = Discord.IMessage;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Handlers;

public sealed class TranslateAutoMessageCommandHandlerTests
{
    private const string ReplyText = "test";
    private const ulong BotUserId = 1UL;
    private readonly ITranslationProviderFactory _translationProviderFactory;
    private readonly IMessageCommandInteraction _interaction;
    private readonly IMessage _message;
    private readonly MessageCommandExecutedNotification _notification;
    private readonly TranslateAutoMessageCommandHandler _sut;

    public TranslateAutoMessageCommandHandlerTests()
    {
        var client = Substitute.For<IDiscordClient>();
        client.CurrentUser.Id.Returns(BotUserId);

        _translationProviderFactory = Substitute.For<ITranslationProviderFactory>();

        _message = Substitute.For<IMessage>();
        _message.Author.Id.Returns(2UL);
        _message.Channel.Returns(Substitute.For<IMessageChannel>());

        var messageHelper = Substitute.For<IMessageHelper>();
        messageHelper
            .BuildTranslationReplyWithReference(_message, Arg.Any<TranslationResult>(), Arg.Any<ulong?>())
            .Returns(ReplyText);

        var data = Substitute.For<IMessageCommandInteractionData>();
        data.Name.Returns(MessageCommandConstants.TranslateAuto.CommandName);
        data.Message.Returns(_message);

        _interaction = Substitute.For<IMessageCommandInteraction>();
        _interaction.Data.Returns(data);

        _notification = new MessageCommandExecutedNotification { Interaction = _interaction };

        _sut = new TranslateAutoMessageCommandHandler(
            client,
            _translationProviderFactory,
            messageHelper,
            new LoggerFake<TranslateAutoMessageCommandHandler>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_MessageCommandExecutedNotification_Success(bool exactSupportedLanguage)
    {
        // Arrange
        _message.Content.Returns("text");
        _interaction.UserLocale.Returns("en-US");

        var supportedLanguage = new SupportedLanguage
        {
            LangCode = exactSupportedLanguage ? "en-US" : "en",
            Name = "English"
        };

        Func<ITranslationProvider, CancellationToken, Task<TranslationResult?>>? action = null;
        _translationProviderFactory
            .WhenForAnyArgs(x => x.TranslateAsync(default!, TestContext.Current.CancellationToken))
            .Do(x => action = x.Arg<Func<ITranslationProvider, CancellationToken, Task<TranslationResult?>>>());

        var expectedTranslationResult = new TranslationResult
        {
            DetectedLanguageCode = "fr",
            DetectedLanguageName = "French",
            TargetLanguageCode = supportedLanguage.LangCode,
            TargetLanguageName = supportedLanguage.Name,
            TranslatedText = "translated text"
        };

        _translationProviderFactory
            .TranslateAsync(default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(expectedTranslationResult);

        var translationProvider = Substitute.For<ITranslationProvider>();
        translationProvider.SupportedLanguages.Returns(new HashSet<SupportedLanguage> { supportedLanguage });

        translationProvider
            .TranslateAsync(supportedLanguage, Arg.Any<string>(), TestContext.Current.CancellationToken)
            .Returns(expectedTranslationResult);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);
        var translationResult = await action!(translationProvider, TestContext.Current.CancellationToken);

        // Assert
        await _translationProviderFactory
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, TestContext.Current.CancellationToken);
        await _interaction.Received(1).DeferAsync(true, Arg.Any<RequestOptions>());
        await _interaction.Received(1).FollowupAsync(ReplyText, ephemeral: true, options: Arg.Any<RequestOptions>());

        _ = translationProvider.Received(exactSupportedLanguage ? 1 : 2).SupportedLanguages;
        translationResult.Should().Be(translationResult);
        await translationProvider
            .Received(1)
            .TranslateAsync(supportedLanguage, Arg.Any<string>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_NotTranslateAutoCommand_Returns()
    {
        // Arrange
        _interaction.Data.Name.Returns("not_the_translate_auto_command");

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        _ = _notification.Interaction.Data.Received(1).Name;
        await _notification.Interaction.DidNotReceiveWithAnyArgs().RespondAsync();
        await _notification.Interaction.DidNotReceiveWithAnyArgs().DeferAsync();
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Returns_WhenSanitizedMessageIsEmpty()
    {
        // Arrange
        _message.Content.Returns(string.Empty);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _interaction.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await _interaction
            .Received(1)
            .RespondAsync("No text to translate.", ephemeral: true, options: Arg.Any<RequestOptions>());

        await _translationProviderFactory
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _interaction.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await _interaction
            .Received(1)
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
        _interaction.UserLocale.Returns(userLocale);

        Func<ITranslationProvider, CancellationToken, Task<TranslationResult?>>? action = null;
        _translationProviderFactory
            .WhenForAnyArgs(x => x.TranslateAsync(default!, TestContext.Current.CancellationToken))
            .Do(x => action = x.Arg<Func<ITranslationProvider, CancellationToken, Task<TranslationResult?>>>());

        _translationProviderFactory
            .TranslateAsync(default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs((TranslationResult?)null);

        var translationProvider = Substitute.For<ITranslationProvider>();
        translationProvider.SupportedLanguages.Returns(new HashSet<SupportedLanguage>());

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);
        var translationResult = await action!(translationProvider, TestContext.Current.CancellationToken);

        // Assert
        await _translationProviderFactory
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, TestContext.Current.CancellationToken);

        await _interaction
            .Received(1)
            .FollowupAsync(
                $"Your locale {userLocale} isn't supported for translation via this action.",
                ephemeral: true,
                options: Arg.Any<RequestOptions>());

        _ = translationProvider.Received(2).SupportedLanguages;
        translationResult.Should().BeNull();
        await translationProvider
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Returns_WhenTranslatedTextIsSame()
    {
        // Arrange
        const string text = "text";

        _message.Content.Returns(text);
        _interaction.UserLocale.Returns("en-US");

        var supportedLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        _translationProviderFactory
            .TranslateAsync(default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    DetectedLanguageCode = "fr",
                    DetectedLanguageName = "French",
                    TargetLanguageCode = supportedLanguage.LangCode,
                    TargetLanguageName = supportedLanguage.Name,
                    TranslatedText = text
                });

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProviderFactory
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, TestContext.Current.CancellationToken);

        await _interaction
            .Received(1)
            .FollowupAsync(
                "The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
                ephemeral: true,
                options: Arg.Any<RequestOptions>());
    }
}
