using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace DiscordTranslationBot.Telemetry;

internal static class TelemetryExtensions
{
    public static void AddTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<Instrumentation>();

        var options = builder.Configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>();
        if (options?.Enabled != true)
        {
            return;
        }

        builder.Logging.AddOpenTelemetry(o =>
        {
            o.IncludeFormattedMessage = true;
            o.IncludeScopes = true;
            o.AddOtlpExporter();
        });

        builder
            .AddOpenTelemetry()
            .ConfigureResource(b =>
                b.AddService(
                    builder.Environment.ApplicationName,
                    serviceVersion: typeof(TelemetryExtensions)
                        .Assembly
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                        ?.InformationalVersion))
            .WithMetrics(b =>
                b
                    .AddMeter(builder.Environment.ApplicationName)
                    .AddProcessInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter())
            .WithTracing(b =>
                b
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(o =>
                        o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/_health", StringComparison.OrdinalIgnoreCase))
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter());
    }
}
