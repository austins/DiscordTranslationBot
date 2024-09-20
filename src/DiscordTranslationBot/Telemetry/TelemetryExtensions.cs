using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DiscordTranslationBot.Telemetry;

internal static class TelemetryExtensions
{
    public static void AddTelemetry(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(TelemetryOptions.SectionName);
        builder.Services.AddOptions<TelemetryOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();

        var options = section.Get<TelemetryOptions>();
        if (options?.Enabled != true)
        {
            return;
        }

        var headers = $"X-Seq-ApiKey={options.ApiKey}";

        builder
            .Services
            .AddOpenTelemetry()
            .ConfigureResource(
                b => b
                    .AddService(builder.Environment.ApplicationName)
                    .AddAttributes(
                        new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName
                        }))
            .WithMetrics(b => b.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddPrometheusExporter())
            .WithTracing(
                b => b
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(
                        e =>
                        {
                            e.Protocol = OtlpExportProtocol.HttpProtobuf;
                            e.Endpoint = options.TracingEndpointUrl!;
                            e.Headers = headers;
                        }));

        builder.Logging.AddOpenTelemetry(
            o =>
            {
                o.IncludeFormattedMessage = true;
                o.IncludeScopes = true;

                o.AddOtlpExporter(
                    e =>
                    {
                        e.Protocol = OtlpExportProtocol.HttpProtobuf;
                        e.Endpoint = options.LoggingEndpointUrl!;
                        e.Headers = headers;
                    });
            });
    }

    public static void UseTelemetry(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<TelemetryOptions>>();
        if (!options.Value.Enabled)
        {
            return;
        }

        app.MapPrometheusScrapingEndpoint("/_metrics");
    }
}
