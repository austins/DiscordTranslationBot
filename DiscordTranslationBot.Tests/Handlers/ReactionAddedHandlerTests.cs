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
using NeoSmart.Unicode;
using Xunit;
using Emoji = Discord.Emoji;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class ReactionAddedHandlerTests
{
    private readonly Mock<TranslationProviderBase> _translationProvider;
    private readonly Mock<IFlagEmojiService> _flagEmojiService;

    private readonly ReactionAddedHandler _sut;
    private readonly Mock<IUserMessage> _message;
    private readonly ReactionAddedNotification _notification;

    public ReactionAddedHandlerTests()
    {
        _translationProvider = new Mock<TranslationProviderBase>(MockBehavior.Strict);
        _translationProvider.Setup(x => x.ProviderName).Returns("Test Provider");

        var client = new Mock<DiscordSocketClient>(MockBehavior.Strict);
        client.Setup(x => x.CurrentUser).Returns((SocketSelfUser) null!);

        _flagEmojiService = new Mock<IFlagEmojiService>(MockBehavior.Strict);

        _sut = new ReactionAddedHandler(
            new[] { _translationProvider.Object },
            client.Object,
            _flagEmojiService.Object,
            Mock.Of<ILogger<ReactionAddedHandler>>());

        _message = new Mock<IUserMessage>();
        _message.Setup(x => x.Id).Returns(1);
        _message.Setup(x => x.Author.Id).Returns(2);
        _message.Setup(x => x.Content).Returns("test");

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
        _flagEmojiService
            .Setup(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>()))
            .Returns(CountryName.France);

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en", TargetLanguageCode = "fr", TranslatedText = "translated_text",
        };

        _translationProvider
            .Setup(
                x => x.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(translationResult);

        _message
            .Setup(
                x => x.RemoveReactionAsync(
                    It.IsAny<IEmote>(),
                    It.IsAny<IUser>(),
                    It.IsAny<RequestOptions>()))
            .Returns(Task.CompletedTask);

        var replyMessage = new Mock<IUserMessage>(MockBehavior.Strict);

        _message
            .Setup(
                x => x.Channel.SendMessageAsync(
                    It.Is<string>(t => t.Contains(translationResult.TranslatedText, StringComparison.Ordinal)),
                    It.IsAny<bool>(),
                    It.IsAny<Embed>(),
                    It.IsAny<RequestOptions>(),
                    It.IsAny<AllowedMentions>(),
                    It.IsAny<MessageReference>(),
                    It.IsAny<MessageComponent>(),
                    It.IsAny<ISticker[]>(),
                    It.IsAny<Embed[]>(),
                    It.IsAny<MessageFlags>()))
            .ReturnsAsync(replyMessage.Object);

        replyMessage
            .Setup(x => x.DeleteAsync(It.IsAny<RequestOptions>()))
            .Returns(Task.CompletedTask);

        _notification.Reaction.Emote = new Emoji(NeoSmart.Unicode.Emoji.FlagUnitedStates.ToString());

        // Act
        var act = async () => await _sut.Handle(
            _notification,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _flagEmojiService.Verify(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Returns_IfNotEmoji()
    {
        // Arrange
        _notification.Reaction.Emote = new Emoji("not_an_emoji");

        // Act
        var act = async () => await _sut.Handle(
            _notification,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _flagEmojiService.Verify(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>()), Times.Never);

        _message.Verify(
            x => x.RemoveReactionAsync(
                It.IsAny<IEmote>(),
                It.IsAny<ulong>(),
                It.IsAny<RequestOptions>()),
            Times.Never);

        _translationProvider.Verify(
            x => x.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Returns_IfCountryNameNotFound()
    {
        // Arrange
        _notification.Reaction.Emote = new Emoji(NeoSmart.Unicode.Emoji.Cactus.ToString());

        _flagEmojiService.Setup(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>())).Returns((string?) null);

        // Act
        var act = async () => await _sut.Handle(
            _notification,
            CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _flagEmojiService.Verify(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>()), Times.Once);

        _message.Verify(
            x => x.RemoveReactionAsync(
                It.IsAny<IEmote>(),
                It.IsAny<ulong>(),
                It.IsAny<RequestOptions>()),
            Times.Never);

        _translationProvider.Verify(
            x => x.TranslateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Returns_SanitizesMessageEmpty()
    {
        // Arrange
        _flagEmojiService
            .Setup(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>()))
            .Returns(CountryName.France);

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

        _flagEmojiService.Verify(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>()), Times.Once);

        _translationProvider
            .Verify(
                x => x.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task Handle_NoTranslationResult()
    {
        // Arrange
        _flagEmojiService
            .Setup(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>()))
            .Returns(CountryName.France);

        _translationProvider
            .Setup(
                x => x.TranslateAsync(
                    It.IsAny<string>(),
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

        _flagEmojiService.Verify(x => x.GetCountryNameBySequence(It.IsAny<UnicodeSequence>()), Times.Once);

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
