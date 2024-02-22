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
                .AddMediatR(
                    c =>
                    {
                        c.Lifetime = ServiceLifetime.Singleton;
                        c.NotificationPublisherType = typeof(BackgroundNotificationPublisher);

                        c.RegisterServicesFromAssemblyContaining<Program>()
                            .AddOpenBehavior(typeof(ValidationBehavior<,>), ServiceLifetime.Singleton)
                            .AddOpenBehavior(typeof(BackgroundCommandBehavior<,>), ServiceLifetime.Singleton)
                            .AddOpenBehavior(typeof(LogElapsedTimeBehavior<,>), ServiceLifetime.Singleton);
                    })
                .AddHostedService<Worker>();
        })
    .Build()
    .RunAsync();
