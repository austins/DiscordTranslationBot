using Discord;
using DiscordTranslationBot.Commands.DiscordCommands;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Commands.DiscordCommands;

public sealed class RegisterDiscordCommandsValidatorTests
{
    private readonly RegisterDiscordCommandsValidator _sut = new();

    [Fact]
    public void Valid_Command_Validates_WithNoErrors()
    {
        // Arrange
        var command = new RegisterDiscordCommands { Guilds = [Substitute.For<IGuild>()] };

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalid_Command_Guilds_Validates_WithErrors(bool isEmpty)
    {
        // Arrange
        var command = new RegisterDiscordCommands { Guilds = isEmpty ? [] : [Substitute.For<IGuild>()] };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        if (isEmpty)
        {
            result.ShouldHaveValidationErrorFor(x => x.Guilds);
        }
        else
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Guilds);
        }
    }
}
