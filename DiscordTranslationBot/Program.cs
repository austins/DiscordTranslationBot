#pragma warning disable CA1852 // Class must be sealed.
global using MediatR;
using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Services;
using FluentValidation;
using MediatR.NotificationPublishers;
using Quartz;

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
                    c.NotificationPublisherType = typeof(TaskWhenAllPublisher);
                    c.RegisterServicesFromAssemblyContaining<Program>();
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
                .AddQuartz()
                .AddQuartzHostedService()
                .AddHostedService<Worker>();
        }
    )
    .Build()
    .RunAsync();
