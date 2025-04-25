using System.Diagnostics;

namespace DiscordTranslationBot.Telemetry;

internal sealed class Instrumentation : IDisposable
{
    public Instrumentation(IHostEnvironment environment)
    {
        ActivitySource = new ActivitySource(environment.ApplicationName);
    }

    public ActivitySource ActivitySource { get; }

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}
