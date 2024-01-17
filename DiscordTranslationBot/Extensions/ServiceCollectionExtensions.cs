using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Refit;
using AzureTranslatorProvider = DiscordTranslationBot.Providers.Translation.AzureTranslator.AzureTranslatorProvider;

namespace DiscordTranslationBot.Extensions;

/// <summary>
/// Extension methods for a service collection.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds translation providers.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>Service collection.</returns>
    internal static IServiceCollection AddTranslationProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Set up configuration.
        var section = configuration.GetSection(TranslationProvidersOptions.SectionName);

        services.AddOptionsWithFluentValidation<TranslationProvidersOptions, TranslationProvidersOptionsValidator>(
            section);

        // Register translation providers. They are prioritized in the order added.
        var options = section.Get<TranslationProvidersOptions>();

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) })
        };

        if (options?.AzureTranslator.Enabled == true)
        {
            services.AddTransient<AzureTranslatorHeadersHandler>();

            services.AddRefitClient<IAzureTranslatorClient>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = options.AzureTranslator.ApiUrl)
                .AddHttpMessageHandler<AzureTranslatorHeadersHandler>()
                .AddRetryPolicy();

            services.AddSingleton<TranslationProviderBase, AzureTranslatorProvider>();
        }

        if (options?.LibreTranslate.Enabled == true)
        {
            services.AddRefitClient<ILibreTranslateClient>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = options.LibreTranslate.ApiUrl)
                .AddRetryPolicy();

            services.AddSingleton<TranslationProviderBase, LibreTranslateProvider>();
        }

        return services;
    }

    private static void AddRetryPolicy(this IHttpClientBuilder builder)
    {
        builder.AddTransientHttpErrorPolicy(
            b => b.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 2)));
    }
}
