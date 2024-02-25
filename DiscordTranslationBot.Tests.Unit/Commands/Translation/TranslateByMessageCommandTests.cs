using Discord;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

public sealed class TranslateByMessageCommandTests
{
    [Test]
    public void Valid_ValidatesWithoutErrors()
    {
        // Arrange
        var request = new TranslateByMessageCommand { MessageCommand = Substitute.For<IMessageCommandInteraction>() };

        // Act
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Test]
    public void Invalid_ValidatesWithErrors()
    {
        // Arrange
        var request = new TranslateByMessageCommand { MessageCommand = null! };

        // Act
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().HaveCount(1);
    }
}
