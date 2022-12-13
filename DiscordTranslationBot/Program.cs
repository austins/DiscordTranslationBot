using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using Serilog;
using Serilog.Templates;
using Serilog.Templates.Themes;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(
            (builder, services) =>
            {
                var translationProvidersOptionsSection = builder.Configuration.GetRequiredSection(
                    TranslationProvidersOptions.SectionName
                );

                var translationProvidersOptions =
                    translationProvidersOptionsSection.Get<TranslationProvidersOptions>();

                // Initial configuration.
                services
                    .Configure<DiscordOptions>(
                        builder.Configuration.GetRequiredSection(DiscordOptions.SectionName)
                    )
                    .Configure<TranslationProvidersOptions>(
                        builder.Configuration.GetRequiredSection(
                            TranslationProvidersOptions.SectionName
                        )
                    )
                    .AddMediator()
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
        .ConfigureLogging(builder => builder.ClearProviders())
        .UseSerilog(
            (hostingContext, services, loggerConfiguration) =>
                loggerConfiguration.ReadFrom
                    .Configuration(hostingContext.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(
                        new ExpressionTemplate(
                            "[{@t:HH:mm:ss} {@l:u3}{#if SourceContext is not null} {SourceContext}{#end}] {@m}\n{@x}",
                            theme: TemplateTheme.Literate
                        )
                    )
        )
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
    when (ex is InvalidOperationException
        && (
            ex.Message.Contains(DiscordOptions.SectionName, StringComparison.Ordinal)
            || ex.Message.Contains(
                TranslationProvidersOptions.SectionName,
                StringComparison.Ordinal
            )
        )
    )
{
    // The app is missing configuration options.
    Log.Fatal(
        $"The {DiscordOptions.SectionName} and {TranslationProvidersOptions.SectionName} options are not configured or set to an invalid format."
    );
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unexpected error occurred.");
}
finally
{
    Log.CloseAndFlush();
}
