﻿#pragma warning disable CA1852 // Class must be sealed.
global using MediatR;
using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Services;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        (builder, services) =>
        {
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
                    c.RegisterServicesFromAssemblyContaining<Program>();
                })
                .AddHttpClient()
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
                            MessageCacheSize = 100
                        }
                    )
                )
                .AddSingleton<DiscordEventListener>()
                .AddHostedService<Worker>();
        }
    )
    .ConfigureLogging(builder => builder.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss "))
    .Build()
    .RunAsync();
