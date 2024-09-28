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

        var openTelemetryBuilder = builder
            .Services
            .AddOpenTelemetry()
            .ConfigureResource(
                b => b
                    .AddService(builder.Environment.ApplicationName)
                    .AddAttributes(
                        new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName
                        }));

        if (options?.MetricsEndpoint.Enabled == true)
        {
            openTelemetryBuilder.WithMetrics(
                b => b
                    .AddProcessInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(e => SetOltpExporterOptions(e, options.MetricsEndpoint)));
        }

        if (options?.TracingEndpoint.Enabled == true)
        {
            openTelemetryBuilder.WithTracing(
                b => b
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(e => SetOltpExporterOptions(e, options.TracingEndpoint)));
        }

        if (options?.LoggingEndpoint.Enabled == true)
        {
            builder.Logging.AddOpenTelemetry(
                o =>
                {
                    o.IncludeFormattedMessage = true;
                    o.IncludeScopes = true;
                    o.AddOtlpExporter(e => SetOltpExporterOptions(e, options.LoggingEndpoint));
                });
        }
    }

    private static void SetOltpExporterOptions(
        OtlpExporterOptions otlpExporterOptions,
        TelemetryEndpointOptions telemetryEndpointOptions)
    {
        otlpExporterOptions.Protocol = telemetryEndpointOptions.Protocol;
        otlpExporterOptions.Endpoint = telemetryEndpointOptions.Url!;
        otlpExporterOptions.Headers = string.Join(
            ';',
            telemetryEndpointOptions.Headers.Select(x => $"{x.Key}={x.Value}"));
    }
}
