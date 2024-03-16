using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Countries.Services;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using MediatR;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

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

    private const ulong BotUserId = 1UL;
    private const ulong MessageUserId = 2UL;

    private readonly ICountryService _countryService;
    private readonly IMediator _mediator;
    private readonly IUserMessage _message;
    private readonly TranslateByCountryFlagEmojiReactionHandler _sut;
    private readonly TranslationProviderBase _translationProvider;

    public TranslateByCountryFlagEmojiReactionHandlerTests()
    {
        _translationProvider = Substitute.For<TranslationProviderBase>();
        _translationProvider.ProviderName.Returns("Test Provider");

        var client = Substitute.For<IDiscordClient>();
        client.CurrentUser.Id.Returns(BotUserId);

        _countryService = Substitute.For<ICountryService>();

        _message = Substitute.For<IUserMessage>();
        _message.Id.Returns(1UL);
        _message.Author.Id.Returns(MessageUserId);
        _message.Content.Returns(Content);

        _message
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>())
            .Returns(Task.CompletedTask);

        var channel = Substitute.For<IMessageChannel, IGuildChannel>();
        channel.EnterTypingState().ReturnsForAnyArgs(Substitute.For<IDisposable>());
        _message.Channel.Returns(channel);
        ((IGuildChannel)channel).Guild.Id.Returns(1UL);

        _mediator = Substitute.For<IMediator>();

        _sut = new TranslateByCountryFlagEmojiReactionHandler(
            client,
            new[] { _translationProvider },
            _countryService,
            _mediator,
            new LoggerFake<TranslateByCountryFlagEmojiReactionHandler>());
    }

    [Fact]
    public async Task Handle_ReactionAddedEvent_Returns_GetCountryByEmojiFalse()
    {
        // Arrange
        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Channel = Substitute.For<IMessageChannel>(),
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.Name)
            }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>()).Returns(false);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<TranslateByCountryFlagEmojiReaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReactionAddedEvent_SendsCommand_GetCountryByEmojiTrue()
    {
        // Arrange
        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Channel = Substitute.For<IMessageChannel>(),
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.Name)
            }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>()).Returns(true);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<TranslateByCountryFlagEmojiReaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        var request = new TranslateByCountryFlagEmojiReaction
        {
            Country = country,
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _translationProvider
            .DidNotReceive()
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Success()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = "translated_text"
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .Returns(translationResult);

        var request = new TranslateByCountryFlagEmojiReaction
        {
            Country = country,
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<SendTempReply>(x => x.Text.Contains("Translated message from", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _message.Content.Returns(string.Empty);

        var request = new TranslateByCountryFlagEmojiReaction
        {
            Country = country,
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _translationProvider
            .DidNotReceive()
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_NoTranslationResult()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TranslationResult)null!);

        var request = new TranslateByCountryFlagEmojiReaction
        {
            Country = country,
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task
        Handle_TranslateByCountryFlagEmojiReaction_TempReplySent_WhenUnsupportedCountryExceptionIsThrown_ForLastTranslationProvider()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string exMessage = "exception message";

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .ThrowsAsync(new LanguageNotSupportedForCountryException(exMessage));

        var request = new TranslateByCountryFlagEmojiReaction
        {
            Country = country,
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator.Received(1).Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TranslateByCountryFlagEmojiReaction_TempReplySent_OnFailureToDetectSourceLanguage()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = ExpectedSanitizedMessage
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .Returns(translationResult);

        var request = new TranslateByCountryFlagEmojiReaction
        {
            Country = country,
            Message = _message,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<SendTempReply>(
                    x => x.Text == "Couldn't detect the source language to translate from or the result is the same."),
                Arg.Any<CancellationToken>());
    }
}
