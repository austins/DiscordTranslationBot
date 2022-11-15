using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using MediatR;

try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(
            (hostBuilder, services) =>
            {
                var translationProvidersOptionsSection =
                    hostBuilder.Configuration.GetRequiredSection(TranslationProvidersOptions.SectionName);

                var translationProvidersOptions = translationProvidersOptionsSection.Get<TranslationProvidersOptions>();

                // Initial configuration.
                services
                    .Configure<DiscordOptions>(hostBuilder.Configuration.GetRequiredSection(DiscordOptions.SectionName))
                    .Configure<TranslationProvidersOptions>(
                        hostBuilder.Configuration.GetRequiredSection(TranslationProvidersOptions.SectionName))
                    .AddMediatR(typeof(Program))
                    .AddHttpClient()
                    .AddSingleton<ICountryService, CountryService>();

                // Register translation providers. They are injected in the order added.
                if (translationProvidersOptions!.AzureTranslator.ApiUrl != null)
                {
                    services.AddSingleton<TranslationProviderBase, AzureTranslatorProvider>();
                }

                services.AddSingleton<TranslationProviderBase, LibreTranslateProvider>();

                // Other services.
                services
                    .AddSingleton(
                        new DiscordSocketClient(
                            new DiscordSocketConfig
                            {
                                GatewayIntents = GatewayIntents.Guilds |
                                                 GatewayIntents.GuildMessages |
                                                 GatewayIntents.GuildMessageReactions |
                                                 GatewayIntents.MessageContent,
                                MessageCacheSize = 100,
                            }))
                    .AddSingleton<DiscordEventListener>()
                    .AddHostedService<Worker>();
            })
        .Build();

    await host.RunAsync();
}
catch (Exception ex) when (ex is InvalidOperationException &&
                           (ex.Message.Contains(DiscordOptions.SectionName, StringComparison.Ordinal) ||
                            ex.Message.Contains(TranslationProvidersOptions.SectionName, StringComparison.Ordinal)))
{
    // The app is missing configuration options.
    Console.WriteLine(
        $"The {DiscordOptions.SectionName} and {TranslationProvidersOptions.SectionName} options are not configured or set to an invalid format.");
}
