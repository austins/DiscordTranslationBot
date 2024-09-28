using System.ComponentModel.DataAnnotations;
using OpenTelemetry.Exporter;

namespace DiscordTranslationBot.Telemetry;

public sealed class TelemetryEndpointOptions : IValidatableObject
{
    /// <summary>
    /// Flag indicating whether emitting telemetry to this endpoint is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Protocol for the endpoint.
    /// </summary>
    public OtlpExportProtocol Protocol { get; init; } = OtlpExportProtocol.HttpProtobuf;

    /// <summary>
    /// URL for the endpoint.
    /// </summary>
    public Uri? Url { get; init; }

    /// <summary>
    /// Headers for the endpoint.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Enabled)
        {
            if (Url?.IsAbsoluteUri != true)
            {
                yield return new ValidationResult(
                    $"{nameof(TelemetryEndpointOptions)}.{nameof(Url)} is must be an absolute URI.",
                    [nameof(Url)]);
            }

            if (Headers.Any(
                    x => x.Key.Contains(';', StringComparison.Ordinal)
                         || x.Value.Contains(';', StringComparison.Ordinal)))
            {
                yield return new ValidationResult(
                    $"{nameof(TelemetryEndpointOptions)}.{nameof(Headers)} cannot contain semicolons.",
                    [nameof(Headers)]);
            }
        }
    }
}
