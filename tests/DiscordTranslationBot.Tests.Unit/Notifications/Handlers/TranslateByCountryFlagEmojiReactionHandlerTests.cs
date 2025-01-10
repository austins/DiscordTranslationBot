using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Notifications.Handlers;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;
using Mediator;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Handlers;

public sealed class TranslateByCountryFlagEmojiReactionHandlerTests
{
    private const string Content = """
                                   👍 test<:disdainsam:630009232128868353> _test_*test*
                                   > test
                                   __test__
                                   """;

    private const string ExpectedSanitizedMessage = """
                                                    test testtest
                                                    test
                                                    test
                                                    """;

    private const string ReplyText = "test";
    private const ulong BotUserId = 1UL;
    private const ulong MessageUserId = 2UL;
    private readonly IMessageChannel _channel;
    private readonly IUserMessage _message;
    private readonly ReactionAddedNotification _notification;

    private readonly ISender _sender;
    private readonly TranslateByCountryFlagEmojiReactionHandler _sut;
    private readonly ITranslationProvider _translationProvider;

    public TranslateByCountryFlagEmojiReactionHandlerTests()
    {
        _translationProvider = Substitute.For<ITranslationProvider>();

        var client = Substitute.For<IDiscordClient>();
        client.CurrentUser.Id.Returns(BotUserId);

        _message = Substitute.For<IUserMessage>();
        _message.Id.Returns(1UL);
        _message.Author.Id.Returns(MessageUserId);
        _message.Content.Returns(Content);

        _message
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>())
            .Returns(Task.CompletedTask);

        _channel = Substitute.For<IMessageChannel, IGuildChannel>();
        _channel.EnterTypingState().ReturnsForAnyArgs(Substitute.For<IDisposable>());
        _message.Channel.Returns(_channel);
        ((IGuildChannel)_channel).Guild.Id.Returns(1UL);

        _sender = Substitute.For<ISender>();

        var messageHelper = Substitute.For<IMessageHelper>();
        messageHelper
            .BuildTranslationReplyWithReference(_message, Arg.Any<TranslationResult>(), Arg.Any<ulong?>())
            .Returns(ReplyText);

        _notification = new ReactionAddedNotification
        {
            Message = _message,
            Channel = _channel,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        var translationProviderFactory = Substitute.For<ITranslationProviderFactory>();
        translationProviderFactory.Providers.Returns([_translationProvider]);
        translationProviderFactory.LastProvider.Returns(_translationProvider);

        _sut = new TranslateByCountryFlagEmojiReactionHandler(
            client,
            translationProviderFactory,
            _sender,
            messageHelper,
            new LoggerFake<TranslateByCountryFlagEmojiReactionHandler>());
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Returns_GetCountryByEmoji_NotAFlagEmoji()
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Channel = _channel,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.Airplane.Name)
            }
        };

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await _message
            .DidNotReceive()
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _translationProvider
            .DidNotReceiveWithAnyArgs()
            .TranslateByCountryAsync(default!, default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _translationProvider
            .DidNotReceiveWithAnyArgs()
            .TranslateByCountryAsync(default!, default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Success()
    {
        // Arrange
        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = "translated_text"
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .Returns(translationResult);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _sender.Received(1).Send(Arg.Is<SendTempReply>(x => x.Text == ReplyText), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        _message.Content.Returns(string.Empty);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProvider
            .DidNotReceiveWithAnyArgs()
            .TranslateByCountryAsync(default!, default!, Arg.Any<CancellationToken>());

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_NoTranslationResult()
    {
        // Arrange
        _translationProvider
            .TranslateByCountryAsync(default!, default!, Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs((TranslationResult)null!);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _sender.DidNotReceiveWithAnyArgs().Send(default!, Arg.Any<CancellationToken>());
        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task
        Handle_TranslateByCountryFlagEmojiReaction_TempReplySent_WhenUnsupportedCountryExceptionIsThrown_ForLastTranslationProvider()
    {
        // Arrange
        const string exMessage = "exception message";

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .ThrowsAsync(new LanguageNotSupportedForCountryException(exMessage));

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _sender.Received(1).Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_TempReplySent_OnFailureToDetectSourceLanguage()
    {
        // Arrange
        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = ExpectedSanitizedMessage
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .Returns(translationResult);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _sender
            .Received(1)
            .Send(
                Arg.Is<SendTempReply>(
                    x => x.Text == "Couldn't detect the source language to translate from or the result is the same."),
                Arg.Any<CancellationToken>());
    }
}
