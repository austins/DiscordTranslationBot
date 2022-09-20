using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class FlagReactionAddedHandlerTests
{
    private const string Content = @"👍 test<:disdainsam:630009232128868353> _test_*test*
> test
__test__";

    private const string ExpectedSanitizedMessage = @"👍 test testtest
 test
test";

    private readonly Mock<TranslationProviderBase> _translationProvider;
    private readonly Mock<ICountryService> _countryService;

    private readonly FlagReactionAddedHandler _sut;
    private readonly Mock<IUserMessage> _message;
    private readonly ReactionAddedNotification _notification;

    public FlagReactionAddedHandlerTests()
    {
        _translationProvider = new Mock<TranslationProviderBase>(MockBehavior.Strict);
        _translationProvider.Setup(x => x.ProviderName).Returns("Test Provider");

        var client = new Mock<DiscordSocketClient>(MockBehavior.Strict);
        client.Setup(x => x.CurrentUser).Returns((SocketSelfUser)null!);

        _countryService = new Mock<ICountryService>(MockBehavior.Strict);

        _sut = new FlagReactionAddedHandler(
            new[] { _translationProvider.Object },
            client.Object,
            _countryService.Object,
            Mock.Of<ILogger<FlagReactionAddedHandler>>());

        _message = new Mock<IUserMessage>();
        _message.Setup(x => x.Id).Returns(1);
        _message.Setup(x => x.Author.Id).Returns(2);
        _message.Setup(x => x.Content).Returns(Content);

        var channel = new Mock<IMessageChannel>();

        _message
            .Setup(x => x.Channel)
            .Returns(channel.Object);

        _notification = new ReactionAddedNotification
        {
            Message = Task.FromResult(_message.Object),
            Channel = Task.FromResult(channel.Object),
            Reaction = new Reaction { UserId = 1UL, Emote = new Emoji("not_an_emoji"), },
        };
    }

    [Fact]
    public async Task Handle_Success()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

        _countryService
            .Setup(x => x.TryGetCountry(It.IsAny<string>(), out country))
            .Returns(true);

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = "translated_text",
        };

        _translationProvider
            .Setup(
                x => x.TranslateAsync(
                    It.IsAny<Country>(),
                    ExpectedSanitizedMessage,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(translationResult);

        _message
            .Setup(
                x => x.RemoveReactionAsync(
                    It.IsAny<IEmote>(),
                    It.IsAny<IUser>(),
                    It.IsAny<RequestOptions>()))
            .Returns(Task.CompletedTask);

        _notification.Reaction.Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString());

        // Act
        var act = async () => await _sut.Handle(
            _notification,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _translationProvider
            .Verify(
                x => x.TranslateAsync(
                    It.IsAny<Country>(),
                    ExpectedSanitizedMessage,
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _countryService.Verify(x => x.TryGetCountry(It.IsAny<string>(), out country), Times.Once);
    }

    [Fact]
    public async Task Handle_Returns_IfNotEmoji()
    {
        // Arrange
        Country? country = null;

        _notification.Reaction.Emote = new Emoji("not_an_emoji");

        // Act
        var act = async () => await _sut.Handle(
            _notification,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _countryService.Verify(x => x.TryGetCountry(It.IsAny<string>(), out country), Times.Never);

        _message.Verify(
            x => x.RemoveReactionAsync(
                It.IsAny<IEmote>(),
                It.IsAny<ulong>(),
                It.IsAny<RequestOptions>()),
            Times.Never);

        _translationProvider.Verify(
            x => x.TranslateAsync(
                It.IsAny<Country>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Returns_IfCountryNameNotFound()
    {
        // Arrange
        _notification.Reaction.Emote = new Emoji(NeoSmart.Unicode.Emoji.Cactus.ToString());

        Country? country = null;
        _countryService.Setup(x => x.TryGetCountry(It.IsAny<string>(), out country)).Returns(false);

        // Act
        var act = async () => await _sut.Handle(
            _notification,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _countryService.Verify(x => x.TryGetCountry(It.IsAny<string>(), out country), Times.Once);

        _message.Verify(
            x => x.RemoveReactionAsync(
                It.IsAny<IEmote>(),
                It.IsAny<ulong>(),
                It.IsAny<RequestOptions>()),
            Times.Never);

        _translationProvider.Verify(
            x => x.TranslateAsync(
                It.IsAny<Country>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

        _countryService
            .Setup(x => x.TryGetCountry(It.IsAny<string>(), out country))
            .Returns(true);

        _message.Setup(x => x.Content).Returns(string.Empty);

        _message
            .Setup(
                x => x.RemoveReactionAsync(
                    It.IsAny<IEmote>(),
                    It.IsAny<IUser>(),
                    It.IsAny<RequestOptions>()))
            .Returns(Task.CompletedTask);

        _notification.Reaction.Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString());

        // Act
        var act = async () => await _sut.Handle(
            _notification,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _countryService.Verify(x => x.TryGetCountry(It.IsAny<string>(), out country), Times.Once);

        _translationProvider
            .Verify(
                x => x.TranslateAsync(
                    It.IsAny<Country>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task Handle_NoTranslationResult()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

        _countryService
            .Setup(x => x.TryGetCountry(It.IsAny<string>(), out country))
            .Returns(true);

        _translationProvider
            .Setup(
                x => x.TranslateAsync(
                    It.IsAny<Country>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationResult)null!);

        _message
            .Setup(
                x => x.RemoveReactionAsync(
                    It.IsAny<IEmote>(),
                    It.IsAny<IUser>(),
                    It.IsAny<RequestOptions>()))
            .Returns(Task.CompletedTask);

        _notification.Reaction.Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString());

        // Act
        var act = async () => await _sut.Handle(
            _notification,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _countryService.Verify(x => x.TryGetCountry(It.IsAny<string>(), out country), Times.Once);

        _message
            .Verify(
                x => x.Channel.SendMessageAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<Embed>(),
                    It.IsAny<RequestOptions>(),
                    It.IsAny<AllowedMentions>(),
                    It.IsAny<MessageReference>(),
                    It.IsAny<MessageComponent>(),
                    It.IsAny<ISticker[]>(),
                    It.IsAny<Embed[]>(),
                    It.IsAny<MessageFlags>()),
                Times.Never());
    }
}
