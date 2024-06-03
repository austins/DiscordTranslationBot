using Discord;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

public sealed class TranslateByMessageCommandTests
{
    [Fact]
    public void Valid_Command_Validates_WithNoErrors()
    {
        // Arrange
        var command = new TranslateByMessageCommand { MessageCommand = Substitute.For<IMessageCommandInteraction>() };

        // Act
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_Command_Validates_WithError()
    {
        // Arrange
        var command = new TranslateByMessageCommand { MessageCommand = null! };

        // Act
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(command.MessageCommand)));
    }
}
