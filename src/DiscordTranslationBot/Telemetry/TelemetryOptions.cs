using System.ComponentModel.DataAnnotations;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Telemetry;

public sealed class TelemetryOptions : IValidatableObject
{
    /// <summary>
    /// Configuration section name for <see cref="TelemetryOptions" />.
    /// </summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// The endpoint options for metrics.
    /// </summary>
    public TelemetryEndpointOptions MetricsEndpoint { get; init; } = new();

    /// <summary>
    /// The endpoint options for logging.
    /// </summary>
    public TelemetryEndpointOptions LoggingEndpoint { get; init; } = new();

    /// <summary>
    /// The endpoint options for tracing.
    /// </summary>
    public TelemetryEndpointOptions TracingEndpoint { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        MetricsEndpoint.TryValidate(out var metricsEndpointValidationResults);
        LoggingEndpoint.TryValidate(out var loggingEndpointValidationResults);
        TracingEndpoint.TryValidate(out var tracingEndpointValidationResults);

        return
        [
            ..metricsEndpointValidationResults,
            ..loggingEndpointValidationResults,
            ..tracingEndpointValidationResults
        ];
    }
}
