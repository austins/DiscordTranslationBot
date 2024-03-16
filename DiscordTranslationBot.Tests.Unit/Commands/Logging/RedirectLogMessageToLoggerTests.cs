using Discord;
using DiscordTranslationBot.Commands.Logging;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Commands.Logging;

public sealed class RedirectLogMessageToLoggerTests
{
    [Fact]
    public void Valid_Command_Validates_WithNoErrors()
    {
        // Arrange
        var request = new RedirectLogMessageToLogger { LogMessage = new LogMessage(LogSeverity.Info, "test", "test") };

        // Act
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}
