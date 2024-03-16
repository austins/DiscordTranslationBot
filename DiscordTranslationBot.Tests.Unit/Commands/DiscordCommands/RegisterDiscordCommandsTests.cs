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
        var request = new RegisterDiscordCommands { Guilds = [Substitute.For<IGuild>()] };

        // Act
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Invalid_Command_Guilds_Validates_WithErrors(bool isNull)
    {
        // Arrange
        var request = new RegisterDiscordCommands { Guilds = isNull ? null! : [] };

        // Act
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().HaveCount(1);
    }
}
