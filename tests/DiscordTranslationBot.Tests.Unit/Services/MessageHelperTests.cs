using Discord;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;

namespace DiscordTranslationBot.Tests.Unit.Services;

public sealed class MessageHelperTests
{
    private const string MessageJumpUrl = "https://discord.com/channels/4/3/1";

    private readonly MessageHelper _sut = new();

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

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = detectedLanguageCode,
            DetectedLanguageName = detectedLanguageCode is null ? null : "English",
            TargetLanguageCode = "de",
            TargetLanguageName = "German",
            TranslatedText = "TRANSLATED_TEXT"
        };

        var expected =
            $"{(interactionUserId is null ? "You" : $"<@{interactionUserId}>")} translated {MessageJumpUrl} by <@{authorId}>";

        if (detectedLanguageCode is not null)
        {
            expected += $" from *{translationResult.DetectedLanguageName}*";
        }

        expected += $" to *{translationResult.TargetLanguageName}*:\n>>> {translationResult.TranslatedText}";

        // Act
        var result = _sut.BuildTranslationReplyWithReference(message, translationResult, interactionUserId);

        // Assert
        result.Should().Be(expected);
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

        var translationResult = new TranslationResult
        {
            DetectedLanguageCode = "en-US",
            DetectedLanguageName = "English",
            TargetLanguageCode = "de",
            TargetLanguageName = "German",
            TranslatedText = "TRANSLATED_TEXT"
        };

        var expected =
            $"<@{interactionUserId}> translated {MessageJumpUrl} from *{translationResult.DetectedLanguageName}* to *{translationResult.TargetLanguageName}*:\n>>> {translationResult.TranslatedText}";

        // Act
        var result = _sut.BuildTranslationReplyWithReference(message, translationResult, interactionUserId);

        // Assert
        result.Should().Be(expected);
    }
}
