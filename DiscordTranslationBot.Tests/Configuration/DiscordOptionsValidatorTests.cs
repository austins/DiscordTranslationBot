using DiscordTranslationBot.Configuration;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Configuration;

[TestClass]
public sealed class DiscordOptionsValidatorTests
{
    private readonly DiscordOptionsValidator _sut = new();

    [TestMethod]
    public void Valid_Options_ValidatesWithoutErrors()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "token" };

        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void Invalid_BotToken_HasValidationErrors(string? botToken)
    {
        // Arrange
        var options = new DiscordOptions { BotToken = botToken! };

        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BotToken).Only();
    }
}
