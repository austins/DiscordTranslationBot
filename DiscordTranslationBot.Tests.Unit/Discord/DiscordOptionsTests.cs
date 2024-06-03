using DiscordTranslationBot.Discord;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Discord;

public sealed class DiscordOptionsTests
{
    [Fact]
    public void Valid_Options_ValidatesWithoutErrors()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "token" };

        // Act
        var isValid = options.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Invalid_BotToken_HasValidationError(string? botToken)
    {
        // Arrange
        var options = new DiscordOptions { BotToken = botToken! };

        // Act
        var isValid = options.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(options.BotToken)));
    }
}
