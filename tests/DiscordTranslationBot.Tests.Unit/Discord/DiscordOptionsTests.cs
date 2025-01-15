using DiscordTranslationBot.Discord;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Discord;

public sealed class DiscordOptionsTests
{
    [Test]
    public void Valid_Options_ValidatesWithoutErrors()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "token" };

        // Act
        var isValid = options.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    public void Invalid_BotToken_HasValidationError(string? botToken)
    {
        // Arrange
        var options = new DiscordOptions { BotToken = botToken! };

        // Act
        var isValid = options.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeFalse();

        var result = validationResults.ShouldHaveSingleItem();
        var memberName = result.MemberNames.ShouldHaveSingleItem();
        memberName.ShouldBe(nameof(options.BotToken));
    }
}
