namespace DiscordTranslationBot.Telemetry;

internal sealed class TelemetryOptions
{
    /// <summary>
    /// Configuration section name for <see cref="TelemetryOptions" />.
    /// </summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Flag indicating whether telemetry is enabled.
    /// </summary>
    public bool Enabled { get; init; }
}
