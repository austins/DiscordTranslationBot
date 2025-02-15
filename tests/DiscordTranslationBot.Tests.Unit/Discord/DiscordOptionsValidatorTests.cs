using DiscordTranslationBot.Discord;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Discord;

public sealed class DiscordOptionsValidatorTests
{
    private readonly DiscordOptionsValidator _sut = new();

    [Test]
    public void Valid_Options_ValidatesWithoutErrors()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "token" };

        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BotToken);
    }
}
