using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Telemetry;

namespace DiscordTranslationBot.Tests.Unit.Telemetry;

public sealed class TelemetryOptionsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Valid_Options_ValidatesWithoutErrors(bool enabled)
    {
        // Arrange
        var options = enabled
            ? new TelemetryOptions
            {
                Enabled = true,
                ApiKey = "apikey",
                LoggingEndpointUrl = new Uri("http://localhost:1234"),
                TracingEndpointUrl = new Uri("http://localhost:1234")
            }
            : new TelemetryOptions();

        // Act
        var isValid = options.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Invalid_Options_HasValidationErrors(string? stringValue)
    {
        // Arrange
        var options = new TelemetryOptions
        {
            Enabled = true,
            ApiKey = stringValue,
            LoggingEndpointUrl = null,
            TracingEndpointUrl = null
        };

        // Act
        var isValid = options.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults
            .Should()
            .HaveCount(3)
            .And
            .ContainSingle(
                x => x.ErrorMessage!.Contains($"{nameof(TelemetryOptions)}.{nameof(TelemetryOptions.ApiKey)}"))
            .And
            .ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(TelemetryOptions)}.{nameof(TelemetryOptions.LoggingEndpointUrl)}"))
            .And
            .ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(TelemetryOptions)}.{nameof(TelemetryOptions.TracingEndpointUrl)}"));
    }
}
