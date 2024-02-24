using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Countries.Services;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using MediatR;
using ReactionMetadata = DiscordTranslationBot.Discord.Models.ReactionMetadata;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class FlagEmojiReactionHandlerTests
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
    private readonly FlagEmojiReactionHandler _sut;
    private readonly TranslationProviderBase _translationProvider;

    public FlagEmojiReactionHandlerTests()
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

        _message.RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>())
            .Returns(Task.CompletedTask);

        var channel = Substitute.For<IMessageChannel, IGuildChannel>();
        channel.EnterTypingState().ReturnsForAnyArgs(Substitute.For<IDisposable>());
        _message.Channel.Returns(channel);
        ((IGuildChannel)channel).Guild.Id.Returns(1UL);

        _mediator = Substitute.For<IMediator>();

        _sut = Substitute.For<FlagEmojiReactionHandler>(
            client,
            new[] { _translationProvider },
            _countryService,
            _mediator,
            new LoggerFake<FlagEmojiReactionHandler>());
    }

    [Test]
    public async Task Handle_ReactionAddedNotification_Returns_IfNotEmoji()
    {
        // Arrange
        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Reaction = new ReactionMetadata
            {
                UserId = 1UL,
                Emote = new Emoji("not_an_emoji")
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _countryService.Received(1).TryGetCountryByEmoji(notification.Reaction.Emote.Name, out Arg.Any<Country?>());

        await _message.DidNotReceive()
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _mediator.DidNotReceive().Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ReactionAddedNotification_Returns_IfCountryNotFound()
    {
        // Arrange
        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Reaction = new ReactionMetadata
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>()).Returns(false);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _countryService.Received(1).TryGetCountryByEmoji(notification.Reaction.Emote.Name, out Arg.Any<Country?>());

        await _message.DidNotReceive()
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _mediator.DidNotReceive().Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ReactionAddedNotification_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(
                x =>
                {
                    x[1] = country;
                    return true;
                });

        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Reaction = new ReactionMetadata
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _translationProvider.DidNotReceive()
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ReactionAddedNotification_Success()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(
                x =>
                {
                    x[1] = country;
                    return true;
                });

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = "translated_text"
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .Returns(translationResult);

        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Reaction = new ReactionMetadata
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider.Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator.Received(1)
            .Send(
                Arg.Is<SendTempReply>(x => x.Text.Contains("Translated message from", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ReactionAddedNotification_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(
                x =>
                {
                    x[1] = country;
                    return true;
                });

        _message.Content.Returns(string.Empty);

        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Reaction = new ReactionMetadata
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider.DidNotReceive()
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Test]
    public async Task Handle_ReactionAddedNotification_NoTranslationResult()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(
                x =>
                {
                    x[1] = country;
                    return true;
                });

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TranslationResult)null!);

        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Reaction = new ReactionMetadata
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Test]
    public async Task
        Handle_ReactionAddedNotification_TempReplySent_WhenUnsupportedCountryExceptionIsThrown_ForLastTranslationProvider()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(
                x =>
                {
                    x[1] = country;
                    return true;
                });

        const string exMessage = "exception message";

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .ThrowsAsync(new LanguageNotSupportedForCountryException(exMessage));

        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Reaction = new ReactionMetadata
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider.Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator.Received(1).Send(Arg.Any<SendTempReply>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ReactionAddedNotification_TempReplySent_OnFailureToDetectSourceLanguage()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService.TryGetCountryByEmoji(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(
                x =>
                {
                    x[1] = country;
                    return true;
                });

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = ExpectedSanitizedMessage
        };

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .Returns(translationResult);

        var notification = new ReactionAddedEvent
        {
            Message = _message,
            Reaction = new ReactionMetadata
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider.Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _mediator.Received(1)
            .Send(
                Arg.Is<SendTempReply>(
                    x => x.Text == "Couldn't detect the source language to translate from or the result is the same."),
                Arg.Any<CancellationToken>());
    }
}
