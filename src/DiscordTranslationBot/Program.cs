using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Discord;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Jobs;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using DiscordTranslationBot.Telemetry;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateSlimBuilder(args);

// Logging.
builder.Logging.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss.fff ");

// Telemetry.
builder.AddTelemetry();

// Configuration.
builder
    .Services
    .AddOptions<DiscordOptions>()
    .Bind(builder.Configuration.GetRequiredSection(DiscordOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Main services.
builder.Host.UseDefaultServiceProvider(
    o =>
    {
        o.ValidateOnBuild = true;
        o.ValidateScopes = true;
    });

builder
    .Services
    .AddTranslationProviders(builder.Configuration)
    .AddSingleton<IDiscordClient>(
        _ => new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                                 | GatewayIntents.GuildMessages
                                 | GatewayIntents.GuildMessageReactions
                                 | GatewayIntents.MessageContent,
                MessageCacheSize = 100,
                UseInteractionSnowflakeDate = false
            }))
    .AddSingleton<DiscordEventListener>()
    .AddSingleton<IMessageHelper, MessageHelper>()
    .AddJobs()
    .AddHostedService<Worker>();

// Mediator.
builder
    .Services
    .AddMediator(o => o.NotificationPublisherType = typeof(NotificationPublisher))
    .AddSingleton(typeof(IPipelineBehavior<,>), typeof(MessageValidationBehavior<,>))
    .AddSingleton(typeof(IPipelineBehavior<,>), typeof(MessageElapsedTimeLoggingBehavior<,>));

// Health checks.
builder
    .Services
    .AddRateLimiting()
    .AddHealthChecks()
    .AddCheck<DiscordClientHealthCheck>(DiscordClientHealthCheck.HealthCheckName);

var app = builder.Build();

app.UseRateLimiter();

app
    .MapHealthChecks(
        "/_health",
        new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse })
    .RequireRateLimiting(RateLimitingExtensions.HealthCheckRateLimiterPolicyName);

await app.RunAsync();
