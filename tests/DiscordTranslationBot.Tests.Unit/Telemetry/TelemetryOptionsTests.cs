using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Telemetry;
using OpenTelemetry.Exporter;

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
                MetricsEndpoint = new TelemetryEndpointOptions
                {
                    Enabled = true,
                    Protocol = OtlpExportProtocol.HttpProtobuf,
                    Url = new Uri("http://localhost:1234"),
                    Headers = new Dictionary<string, string>()
                },
                LoggingEndpoint = new TelemetryEndpointOptions
                {
                    Enabled = true,
                    Protocol = OtlpExportProtocol.HttpProtobuf,
                    Url = new Uri("http://localhost:1234"),
                    Headers = new Dictionary<string, string>()
                },
                TracingEndpoint = new TelemetryEndpointOptions
                {
                    Enabled = true,
                    Protocol = OtlpExportProtocol.HttpProtobuf,
                    Url = new Uri("http://localhost:1234"),
                    Headers = new Dictionary<string, string>()
                }
            }
            : new TelemetryOptions();

        // Act
        var isValid = options.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_Options_HasValidationErrors()
    {
        // Arrange
        var options = new TelemetryOptions
        {
            MetricsEndpoint = new TelemetryEndpointOptions
            {
                Enabled = true,
                Protocol = OtlpExportProtocol.Grpc,
                Url = null,
                Headers = new Dictionary<string, string> { { "invalid;key", "invalid;value" } }
            },
            LoggingEndpoint = new TelemetryEndpointOptions
            {
                Enabled = true,
                Protocol = OtlpExportProtocol.HttpProtobuf,
                Url = null,
                Headers = new Dictionary<string, string>()
            },
            TracingEndpoint = new TelemetryEndpointOptions
            {
                Enabled = true,
                Protocol = OtlpExportProtocol.HttpProtobuf,
                Url = null,
                Headers = new Dictionary<string, string>()
            }
        };

        // Act
        var isValid = options.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults
            .Should()
            .HaveCount(4)
            .And
            .Contain(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(TelemetryEndpointOptions)}.{nameof(TelemetryEndpointOptions.Url)}"))
            .And
            .ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(TelemetryEndpointOptions)}.{nameof(TelemetryEndpointOptions.Headers)}"));
    }
}
