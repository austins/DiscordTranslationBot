#pragma warning disable CA1852 // Seal internal types
using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Services;

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(builder => builder.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss "))
    .ConfigureServices(
        (builder, services) =>
        {
            // Set up configuration.
            services.AddOptions<DiscordOptions>()
                .Bind(builder.Configuration.GetRequiredSection(DiscordOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Set up services.
            services.AddTranslationProviders(builder.Configuration)
                .AddMediatR(
                    c =>
                    {
                        c.Lifetime = ServiceLifetime.Singleton;
                        c.NotificationPublisherType = typeof(BackgroundPublisher);

                        c.RegisterServicesFromAssemblyContaining<Program>()
                            .AddOpenBehavior(typeof(BackgroundCommandBehavior<,>), ServiceLifetime.Singleton)
                            .AddOpenBehavior(typeof(ValidationBehavior<,>), ServiceLifetime.Singleton);
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
                        }))
                .AddSingleton<DiscordEventListener>()
                .AddHostedService<Worker>();
        })
    .Build()
    .RunAsync();
