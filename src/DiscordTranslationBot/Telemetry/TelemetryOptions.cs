using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Telemetry;

public sealed class TelemetryOptions : IValidatableObject
{
    /// <summary>
    /// Configuration section name for <see cref="TelemetryOptions" />.
    /// </summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Flag indicating whether telemetry is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// The API key for Seq used by <see cref="LoggingEndpointUrl" /> and <see cref="TracingEndpointUrl" />.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// The URL for logging endpoint.
    /// </summary>
    public Uri? LoggingEndpointUrl { get; init; }

    /// <summary>
    /// The URL for tracing endpoint.
    /// </summary>
    public Uri? TracingEndpointUrl { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Enabled)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                yield return new ValidationResult(
                    $"{nameof(TelemetryOptions)}.{nameof(ApiKey)} is required.",
                    [nameof(ApiKey)]);
            }

            if (LoggingEndpointUrl?.IsAbsoluteUri != true)
            {
                yield return new ValidationResult(
                    $"{nameof(TelemetryOptions)}.{nameof(LoggingEndpointUrl)} is must be an absolute URI.",
                    [nameof(LoggingEndpointUrl)]);
            }

            if (TracingEndpointUrl?.IsAbsoluteUri != true)
            {
                yield return new ValidationResult(
                    $"{nameof(TelemetryOptions)}.{nameof(TracingEndpointUrl)} is must be an absolute URI.",
                    [nameof(TracingEndpointUrl)]);
            }
        }
    }
}
