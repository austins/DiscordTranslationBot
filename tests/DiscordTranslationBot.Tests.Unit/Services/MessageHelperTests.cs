using Discord;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;

namespace DiscordTranslationBot.Tests.Unit.Services;

public sealed class MessageHelperTests
{
    private readonly MessageHelper _sut = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetJumpUrl_ReturnsExpected(bool isDmChannel)
    {
        // Arrange
        const ulong messageId = 1UL;
        const ulong channelId = 2UL;

        var message = Substitute.For<IMessage>();
        message.Id.Returns(messageId);

        var expectedUrl = "https://discord.com/channels/";
        if (isDmChannel)
        {
            var channel = Substitute.For<IDMChannel>();
            channel.Id.Returns(channelId);
            message.Channel.Returns(channel);

            expectedUrl += $"{MessageHelper.DmChannelId}/{channelId}/{messageId}";
        }
        else
        {
            var channel = Substitute.For<ITextChannel>();
            const ulong guildId = 3UL;
            channel.GuildId.Returns(guildId);
            channel.Id.Returns(channelId);
            message.Channel.Returns(channel);

            expectedUrl += $"{guildId}/{channelId}/{messageId}";
        }

        var expected = new Uri(expectedUrl, UriKind.Absolute);

        // Act
        var result = _sut.GetJumpUrl(message);

        // Assert
        result.AbsoluteUri.ShouldBe(expected.AbsoluteUri);
    }

    [Fact]
    public void GetJumpUrlsInMessage_ReturnsExpected()
    {
        // Arrange
        const ulong messageId = 1UL;
        const ulong channelId = 2UL;

        var dmMessage = Substitute.For<IMessage>();
        dmMessage.Id.Returns(messageId);
        var dmChannel = Substitute.For<IDMChannel>();
        dmChannel.Id.Returns(channelId);
        dmMessage.Channel.Returns(dmChannel);

        var guildMessage = Substitute.For<IMessage>();
        guildMessage.Id.Returns(messageId);
        var guildChannel = Substitute.For<ITextChannel>();
        const ulong guildId = 3UL;
        guildChannel.GuildId.Returns(guildId);
        guildChannel.Id.Returns(channelId);
        guildMessage.Channel.Returns(guildChannel);

        var dmMessageJumpUrl = _sut.GetJumpUrl(dmMessage);
        var guildMessageJumpUrl = _sut.GetJumpUrl(guildMessage);

        var mainMessage = Substitute.For<IMessage>();
        mainMessage.CleanContent.Returns($"{dmMessageJumpUrl} some text {guildMessageJumpUrl}");

        var expected = new List<JumpUrl>
        {
            new()
            {
                IsDmChannel = true,
                GuildId = null,
                ChannelId = channelId,
                MessageId = messageId
            },
            new()
            {
                IsDmChannel = false,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId
            }
        };

        // Act
        var result = _sut.GetJumpUrlsInMessage(mainMessage);

        // Assert
        result.ShouldBeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(1UL, null)]
    [InlineData(1UL, "en-US")]
    public void BuildTranslationReplyWithReference_ReturnsExpected(
        ulong? interactionUserId,
        string? detectedLanguageCode)
    {
        // Arrange
        var message = Substitute.For<IMessage>();
        message.Id.Returns(1UL);
        const ulong authorId = 2UL;
        message.Author.Id.Returns(authorId);

        var channel = Substitute.For<ITextChannel>();
        channel.Id.Returns(3UL);
        channel.GuildId.Returns(4UL);
        message.Channel.Returns(channel);

        var messageJumpUrl = _sut.GetJumpUrl(message);

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = detectedLanguageCode,
            DetectedLanguageName = detectedLanguageCode is null ? null : "English",
            TargetLanguageCode = "de",
            TargetLanguageName = "German",
            TranslatedText = "TRANSLATED_TEXT"
        };

        var expected =
            $"{(interactionUserId is null ? "You" : $"<@{interactionUserId}>")} translated {messageJumpUrl} by <@{authorId}>";

        if (detectedLanguageCode is not null)
        {
            expected += $" from *{translationResult.DetectedLanguageName}*";
        }

        expected += $" to *{translationResult.TargetLanguageName}*:\n>>> {translationResult.TranslatedText}";

        // Act
        var result = _sut.BuildTranslationReplyWithReference(message, translationResult, interactionUserId);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void BuildTranslationReplyWithReference_SameUser_ReturnsExpected()
    {
        // Arrange
        var message = Substitute.For<IMessage>();
        message.Id.Returns(1UL);

        const ulong interactionUserId = 2UL;
        message.Author.Id.Returns(interactionUserId);

        var channel = Substitute.For<ITextChannel>();
        channel.Id.Returns(3UL);
        channel.GuildId.Returns(4UL);
        message.Channel.Returns(channel);

        var messageJumpUrl = _sut.GetJumpUrl(message);

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en-US",
            DetectedLanguageName = "English",
            TargetLanguageCode = "de",
            TargetLanguageName = "German",
            TranslatedText = "TRANSLATED_TEXT"
        };

        var expected =
            $"<@{interactionUserId}> translated {messageJumpUrl} from *{translationResult.DetectedLanguageName}* to *{translationResult.TargetLanguageName}*:\n>>> {translationResult.TranslatedText}";

        // Act
        var result = _sut.BuildTranslationReplyWithReference(message, translationResult, interactionUserId);

        // Assert
        result.ShouldBe(expected);
    }
}
