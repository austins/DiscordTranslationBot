using Discord;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Jobs;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using Quartz;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class FlagEmojiReactionHandlerTests
{
    private const string Content = """
        👍 test<:disdainsam:630009232128868353> _test_*test*
        > test
        __test__
        """;

    private const string ExpectedSanitizedMessage = """
        👍 test testtest
         test
        test
        """;

    private const ulong BotUserId = 1UL;
    private const ulong MessageUserId = 2UL;

    private readonly ICountryService _countryService;
    private readonly IUserMessage _message;
    private readonly FlagEmojiReactionHandler _sut;
    private readonly TranslationProviderBase _translationProvider;
    private readonly IScheduler _scheduler;

    public FlagEmojiReactionHandlerTests()
    {
        _translationProvider = Substitute.For<TranslationProviderBase>(Substitute.For<IHttpClientFactory>());
        _translationProvider.ProviderName.Returns("Test Provider");

        var botUser = Substitute.For<ISelfUser>();
        botUser.Id.Returns(BotUserId);

        var client = Substitute.For<IDiscordClient>();
        client.CurrentUser.Returns(botUser);

        _countryService = Substitute.For<ICountryService>();

        _message = Substitute.For<IUserMessage>();
        _message.Id.Returns(1UL);
        _message.Author.Id.Returns(MessageUserId);
        _message.Content.Returns(Content);

        _message
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>())
            .Returns(Task.CompletedTask);

        var guild = Substitute.For<IGuild>();
        guild.Id.Returns((ulong)1);

        var channel = Substitute.For<IMessageChannel, IGuildChannel>();
        channel.EnterTypingState().ReturnsForAnyArgs(Substitute.For<IDisposable>());
        _message.Channel.Returns(channel);
        ((IGuildChannel)channel).Guild.Returns(guild);

        _scheduler = Substitute.For<IScheduler>();

        var schedulerFactory = Substitute.For<ISchedulerFactory>();
        schedulerFactory.GetScheduler().Returns(_scheduler);

        _sut = Substitute.For<FlagEmojiReactionHandler>(
            client,
            new[] { _translationProvider },
            _countryService,
            schedulerFactory,
            Substitute.For<ILogger<FlagEmojiReactionHandler>>()
        );
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Returns_IfNotEmoji()
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Reaction = new Reaction { UserId = 1UL, Emote = new Emoji("not_an_emoji") }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _countryService.DidNotReceive().TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>());
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Returns_IfCountryNotFound()
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        _countryService.TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>()).Returns(false);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _countryService.Received(1).TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>());

        await _message
            .DidNotReceive()
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _scheduler
            .DidNotReceive()
            .ScheduleJob(
                Arg.Is<IJobDetail>(x => x.JobType == typeof(DeleteTempReplyForFlagEmojiReactionJob)),
                Arg.Any<ITrigger>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService
            .TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(x =>
            {
                x[1] = country;
                return true;
            });

        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await _translationProvider
            .DidNotReceive()
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Success()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService
            .TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(x =>
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

        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _scheduler
            .Received(1)
            .ScheduleJob(
                Arg.Is<IJobDetail>(x => x.JobType == typeof(DeleteTempReplyForFlagEmojiReactionJob)),
                Arg.Any<ITrigger>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService
            .TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(x =>
            {
                x[1] = country;
                return true;
            });

        _message.Content.Returns(string.Empty);

        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider
            .DidNotReceive()
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_NoTranslationResult()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService
            .TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(x =>
            {
                x[1] = country;
                return true;
            });

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TranslationResult)null!);

        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _scheduler
            .DidNotReceive()
            .ScheduleJob(
                Arg.Is<IJobDetail>(x => x.JobType == typeof(DeleteTempReplyForFlagEmojiReactionJob)),
                Arg.Any<ITrigger>(),
                Arg.Any<CancellationToken>()
            );

        await _message.Received(1).RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_TempReplySent_WhenUnsupportedCountryExceptionIsThrown_ForLastTranslationProvider()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService
            .TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(x =>
            {
                x[1] = country;
                return true;
            });

        const string exMessage = "exception message";

        _translationProvider
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnsupportedCountryException(exMessage));

        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _scheduler
            .Received(1)
            .ScheduleJob(
                Arg.Is<IJobDetail>(x => x.JobType == typeof(DeleteTempReplyForFlagEmojiReactionJob)),
                Arg.Any<ITrigger>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_TempReplySent_OnFailureToDetectSourceLanguage()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService
            .TryGetCountry(Arg.Any<string>(), out Arg.Any<Country?>())
            .Returns(x =>
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

        var notification = new ReactionAddedNotification
        {
            Message = _message,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateByCountryAsync(Arg.Any<Country>(), ExpectedSanitizedMessage, Arg.Any<CancellationToken>());

        await _scheduler
            .Received(1)
            .ScheduleJob(
                Arg.Is<IJobDetail>(x => x.JobType == typeof(DeleteTempReplyForFlagEmojiReactionJob)),
                Arg.Any<ITrigger>(),
                Arg.Any<CancellationToken>()
            );
    }
}
