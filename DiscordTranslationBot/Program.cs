#pragma warning disable CA1852 // Seal internal types
using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.HealthChecks;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss.fff ");

// Set up configuration.
builder.Services.AddOptions<DiscordOptions>()
    .Bind(builder.Configuration.GetRequiredSection(DiscordOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Set up services.
builder.Services.AddTranslationProviders(builder.Configuration)
    .AddSingleton<ICountryService, CountryService>()
    .AddSingleton<IDiscordClient>(
        _ => new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents =
                    GatewayIntents.Guilds
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.GuildMessageReactions
                    | GatewayIntents.MessageContent,
                MessageCacheSize = 100,
                UseInteractionSnowflakeDate = false
            }))
    .AddMediatR(
        c =>
        {
            c.Lifetime = ServiceLifetime.Singleton;
            c.NotificationPublisherType = typeof(NotificationPublisher);

            c.RegisterServicesFromAssemblyContaining<Program>()
                .AddOpenBehavior(typeof(ValidationBehavior<,>), ServiceLifetime.Singleton)
                .AddOpenBehavior(typeof(LogElapsedTimeBehavior<,>), ServiceLifetime.Singleton);
        })
    .AddSingleton<DiscordEventListener>()
    .AddHostedService<Worker>();

builder.Services.AddHealthChecks().AddCheck<DiscordClientHealthCheck>(DiscordClientHealthCheck.HealthCheckName);

var app = builder.Build();

app.MapHealthChecks(
    "/_health",
    new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });

await app.RunAsync();
