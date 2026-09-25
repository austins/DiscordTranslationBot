using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DiscordTranslationBot.Telemetry;

internal sealed class Instrumentation : IDisposable
{
    private readonly Meter _meter;

    public Instrumentation(IHostEnvironment environment)
    {
        ActivitySource = new ActivitySource(environment.ApplicationName);
        _meter = new Meter(environment.ApplicationName);

        TranslationCharacters = _meter.CreateCounter<long>(
            "translation.characters",
            "{character}",
            "Characters of text translated by a provider.");
    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> TranslationCharacters { get; }

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }
}
