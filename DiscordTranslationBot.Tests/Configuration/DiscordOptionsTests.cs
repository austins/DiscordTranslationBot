using DiscordTranslationBot.Configuration;

namespace DiscordTranslationBot.Tests.Configuration;

public sealed class DiscordOptionsTests
{
    [Test]
    public void Valid_Options_ValidatesWithoutErrors()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "token" };

        // Act
        var (results, isValid) = options.ValidateObject();

        // Assert
        results.Should().BeEmpty();
        isValid.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Invalid_BotToken_HasValidationErrors(string? botToken)
    {
        // Arrange
        var options = new DiscordOptions { BotToken = botToken! };

        // Act
        var (results, isValid) = options.ValidateObject();

        // Assert
        results.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(options.BotToken)));
        isValid.Should().BeFalse();
    }
}
