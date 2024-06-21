using Discord;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

public sealed class TranslateBySlashCommandTests
{
    [Fact]
    public void Valid_Command_Validates_WithNoErrors()
    {
        // Arrange
        var command = new TranslateBySlashCommand { SlashCommand = Substitute.For<ISlashCommandInteraction>() };

        // Act
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_Command_Validates_WithError()
    {
        // Arrange
        var command = new TranslateBySlashCommand { SlashCommand = null! };

        // Act
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(command.SlashCommand)));
    }
}
