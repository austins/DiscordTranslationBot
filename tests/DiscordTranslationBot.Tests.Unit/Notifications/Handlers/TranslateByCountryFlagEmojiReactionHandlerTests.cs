using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Notifications.Handlers;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Exceptions;
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
    private readonly ITranslationProviderFactory _translationProviderFactory;
    private readonly IMessageChannel _channel;
    private readonly IUserMessage _message;
    private readonly ReactionAddedNotification _notification;
    private readonly ISender _sender;

    private readonly TranslateByCountryFlagEmojiReactionHandler _sut;

    public TranslateByCountryFlagEmojiReactionHandlerTests()
    {
        _translationProviderFactory = Substitute.For<ITranslationProviderFactory>();

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

        _sut = new TranslateByCountryFlagEmojiReactionHandler(
            client,
            _translationProviderFactory,
            _sender,
            messageHelper,
            new LoggerFake<TranslateByCountryFlagEmojiReactionHandler>());
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Returns_GetCountryByEmoji_NotASupportedCountryFlagEmoji()
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

        await _translationProviderFactory
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, TestContext.Current.CancellationToken);
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

        await _translationProviderFactory
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Success()
    {
        // Arrange
        _translationProviderFactory
            .TranslateAsync(default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    DetectedLanguageCode = "en",
                    TargetLanguageCode = "fr",
                    TranslatedText = "translated_text"
                });

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProviderFactory
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, TestContext.Current.CancellationToken);
        await _sender
            .Received(1)
            .Send(Arg.Is<SendTempReply>(x => x.Text == ReplyText), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        _message.Content.Returns(string.Empty);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProviderFactory
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, TestContext.Current.CancellationToken);
        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_NoTranslationResult()
    {
        // Arrange
        _translationProviderFactory
            .TranslateAsync(default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs((TranslationResult)null!);

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _sender.DidNotReceiveWithAnyArgs().Send(default!, TestContext.Current.CancellationToken);
        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task
        Handle_TranslateByCountryFlagEmojiReaction_TempReplySent_WhenUnsupportedCountryExceptionIsThrown_ForLastTranslationProvider()
    {
        // Arrange
        const string exMessage = "exception message";

        _translationProviderFactory
            .TranslateAsync(default!, TestContext.Current.CancellationToken)
            .ThrowsAsyncForAnyArgs(
                new TranslationFailureException("provider", new LanguageNotSupportedForCountryException(exMessage)));

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProviderFactory
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, TestContext.Current.CancellationToken);
        await _sender.Received(1).Send(Arg.Any<SendTempReply>(), TestContext.Current.CancellationToken);

        await _message
            .DidNotReceive()
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_TempReplySent_OnFailureToDetectSourceLanguage()
    {
        // Arrange
        _translationProviderFactory
            .TranslateAsync(default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    DetectedLanguageCode = "en",
                    TargetLanguageCode = "fr",
                    TranslatedText = ExpectedSanitizedMessage
                });

        // Act
        await _sut.Handle(_notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProviderFactory
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, TestContext.Current.CancellationToken);

        await _sender
            .Received(1)
            .Send(
                Arg.Is<SendTempReply>(x =>
                    x.Text == "Couldn't detect the source language to translate from or the result is the same."),
                TestContext.Current.CancellationToken);
    }
}
