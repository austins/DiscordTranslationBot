using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Commands.ReactionAdded;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using FluentAssertions;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class ReactionAddedHandlerTests
{
    private const string Content =
        @"👍 test<:disdainsam:630009232128868353> _test_*test*
> test
__test__";

    private const string ExpectedSanitizedMessage =
        @"👍 test testtest
 test
test";

    private readonly Mock<IMessageChannel> _channel;

    private readonly Mock<ICountryService> _countryService;
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<IUserMessage> _message;

    private readonly ReactionAddedHandler _sut;

    private readonly Mock<ITranslationProvider> _translationProvider;

    public ReactionAddedHandlerTests()
    {
        _mediator = new Mock<IMediator>(MockBehavior.Strict);

        _translationProvider = new Mock<ITranslationProvider>(MockBehavior.Strict);
        _translationProvider.Setup(x => x.ProviderName).Returns("Test Provider");

        var client = new Mock<DiscordSocketClient>(MockBehavior.Strict);
        client.Setup(x => x.CurrentUser).Returns((SocketSelfUser)null!);

        _countryService = new Mock<ICountryService>(MockBehavior.Strict);

        _sut = new ReactionAddedHandler(
            _mediator.Object,
            new[] { _translationProvider.Object },
            client.Object,
            _countryService.Object,
            Mock.Of<ILogger<ReactionAddedHandler>>()
        );

        _message = new Mock<IUserMessage>(MockBehavior.Strict);
        _message.Setup(x => x.Id).Returns(1);
        _message.Setup(x => x.Author.Id).Returns(2);
        _message.Setup(x => x.Content).Returns(Content);

        _message
            .Setup(
                x =>
                    x.RemoveReactionAsync(
                        It.IsAny<IEmote>(),
                        It.IsAny<ulong>(),
                        It.IsAny<RequestOptions>()
                    )
            )
            .Returns(Task.CompletedTask);

        _channel = new Mock<IMessageChannel>(MockBehavior.Strict);
        _message.Setup(x => x.Channel).Returns(_channel.Object);
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Success()
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = _message.Object,
            Channel = _channel.Object,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        var country = new Country(Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _countryService.Setup(x => x.TryGetCountry(It.IsAny<string>(), out country)).Returns(true);

        _mediator
            .Setup(x => x.Send(It.IsAny<ProcessFlagEmojiReaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mediator.Verify(
            x => x.Send(It.IsAny<ProcessFlagEmojiReaction>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Returns_IfNotEmoji()
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = _message.Object,
            Channel = _channel.Object,
            Reaction = new Reaction { UserId = 1UL, Emote = new Discord.Emoji("not_an_emoji") }
        };

        Country? country = null;

        // Act
        var act = async () => await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _countryService.Verify(x => x.TryGetCountry(It.IsAny<string>(), out country), Times.Never);

        _mediator.Verify(
            x => x.Send(It.IsAny<ProcessFlagEmojiReaction>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ReactionAddedNotification_Returns_IfCountryNotFound()
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = _message.Object,
            Channel = _channel.Object,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        Country? country = null;
        _countryService.Setup(x => x.TryGetCountry(It.IsAny<string>(), out country)).Returns(false);

        // Act
        var act = async () => await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _countryService.Verify(x => x.TryGetCountry(It.IsAny<string>(), out country), Times.Once);

        _mediator.Verify(
            x => x.Send(It.IsAny<ProcessFlagEmojiReaction>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ProcessFlagEmojiReaction_Success()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "France")
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
            .Setup(
                x =>
                    x.TranslateByCountryAsync(
                        It.IsAny<Country>(),
                        ExpectedSanitizedMessage,
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(translationResult);

        _message
            .Setup(
                x =>
                    x.RemoveReactionAsync(
                        It.IsAny<IEmote>(),
                        It.IsAny<IUser>(),
                        It.IsAny<RequestOptions>()
                    )
            )
            .Returns(Task.CompletedTask);

        _channel
            .Setup(x => x.EnterTypingState(It.IsAny<RequestOptions>()))
            .Returns(Mock.Of<IDisposable>());

        var command = new ProcessFlagEmojiReaction
        {
            Message = _message.Object,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            },
            Country = country
        };

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _translationProvider.Verify(
            x =>
                x.TranslateByCountryAsync(
                    It.IsAny<Country>(),
                    ExpectedSanitizedMessage,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _message.Verify(
            x =>
                x.RemoveReactionAsync(
                    It.IsAny<IEmote>(),
                    It.IsAny<ulong>(),
                    It.IsAny<RequestOptions>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ProcessFlagEmojiReaction_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _message.Setup(x => x.Content).Returns(string.Empty);

        _message
            .Setup(
                x =>
                    x.RemoveReactionAsync(
                        It.IsAny<IEmote>(),
                        It.IsAny<IUser>(),
                        It.IsAny<RequestOptions>()
                    )
            )
            .Returns(Task.CompletedTask);

        var command = new ProcessFlagEmojiReaction
        {
            Message = _message.Object,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            },
            Country = country
        };

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _translationProvider.Verify(
            x =>
                x.TranslateByCountryAsync(
                    It.IsAny<Country>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );

        _message.Verify(
            x =>
                x.RemoveReactionAsync(
                    It.IsAny<IEmote>(),
                    It.IsAny<ulong>(),
                    It.IsAny<RequestOptions>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ProcessFlagEmojiReaction_NoTranslationResult()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        _translationProvider
            .Setup(
                x =>
                    x.TranslateByCountryAsync(
                        It.IsAny<Country>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync((TranslationResult)null!);

        _message
            .Setup(
                x =>
                    x.RemoveReactionAsync(
                        It.IsAny<IEmote>(),
                        It.IsAny<IUser>(),
                        It.IsAny<RequestOptions>()
                    )
            )
            .Returns(Task.CompletedTask);

        var command = new ProcessFlagEmojiReaction
        {
            Message = _message.Object,
            Reaction = new Reaction
            {
                UserId = 1UL,
                Emote = new Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            },
            Country = country
        };

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _message.Verify(
            x =>
                x.Channel.SendMessageAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<Embed>(),
                    It.IsAny<RequestOptions>(),
                    It.IsAny<AllowedMentions>(),
                    It.IsAny<MessageReference>(),
                    It.IsAny<MessageComponent>(),
                    It.IsAny<ISticker[]>(),
                    It.IsAny<Embed[]>(),
                    It.IsAny<MessageFlags>()
                ),
            Times.Never
        );

        _message.Verify(
            x =>
                x.RemoveReactionAsync(
                    It.IsAny<IEmote>(),
                    It.IsAny<ulong>(),
                    It.IsAny<RequestOptions>()
                ),
            Times.Once
        );
    }
}
