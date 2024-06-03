using Discord;
using DiscordTranslationBot.Commands.DiscordCommands;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Commands.DiscordCommands;

public sealed class RegisterDiscordCommandsTests
{
    [Fact]
    public void Valid_Command_Validates_WithNoErrors()
    {
        // Arrange
        var command = new RegisterDiscordCommands { Guilds = [Substitute.For<IGuild>()] };

        // Act
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_Command_Guilds_Validates_WithErrors()
    {
        // Arrange
        var command = new RegisterDiscordCommands { Guilds = [] };

        // Act
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(command.Guilds)));
    }
}
