#pragma warning disable CA1852 // Class must be sealed.
global using MediatR;
using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Services;
using FluentValidation;

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(builder => builder.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss "))
    .ConfigureServices(
        (builder, services) =>
        {
            services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

            // Set up configuration.
            services.AddOptionsWithFluentValidation<DiscordOptions, DiscordOptionsValidator>(
                builder.Configuration.GetRequiredSection(DiscordOptions.SectionName)
            );

            // Set up services.
            services
                .AddTranslationProviders(builder.Configuration)
                .AddMediatR(c =>
                {
                    c.Lifetime = ServiceLifetime.Singleton;
                    c.NotificationPublisherType = typeof(BackgroundPublisher);
                    c.RegisterServicesFromAssemblyContaining<Program>();
                    c.AddOpenBehavior(typeof(ValidationBehavior<,>), ServiceLifetime.Singleton);
                })
                .AddSingleton<ICountryService, CountryService>()
                .AddSingleton<IDiscordClient>(
                    new DiscordSocketClient(
                        new DiscordSocketConfig
                        {
                            GatewayIntents =
                                GatewayIntents.Guilds
                                | GatewayIntents.GuildMessages
                                | GatewayIntents.GuildMessageReactions
                                | GatewayIntents.MessageContent,
                            MessageCacheSize = 100,
                            UseInteractionSnowflakeDate = false
                        }
                    )
                )
                .AddSingleton<DiscordEventListener>()
                .AddHostedService<Worker>();
        }
    )
    .Build()
    .RunAsync();
