using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Refit;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Extension methods for a service collection.
/// </summary>
internal static class TranslationProviderExtensions
{
    private static readonly RefitSettings RefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                // Ensure that all unicode characters are serialized correctly.
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            })
    };

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
        services.AddOptionsWithFluentValidation<TranslationProvidersOptions>(section);

        var options = section.Get<TranslationProvidersOptions>();

        // Register translation providers. They are prioritized in the order added.
        if (options?.AzureTranslator.Enabled == true)
        {
            services.AddTranslationProvider<IAzureTranslatorClient, AzureTranslatorProvider>(
                options.AzureTranslator.ApiUrl!,
                [typeof(AzureTranslatorHeadersHandler)]);
        }

        if (options?.LibreTranslate.Enabled == true)
        {
            services.AddTranslationProvider<ILibreTranslateClient, LibreTranslateProvider>(
                options.AzureTranslator.ApiUrl!);
        }

        return services;
    }

    private static void AddTranslationProvider<TRefitClient, TTranslationProvider>(
        this IServiceCollection services,
        Uri apiUrl,
        IReadOnlyList<Type>? delegatingHandlerTypes = null)
        where TRefitClient : class
        where TTranslationProvider : TranslationProviderBase
    {
        var httpClientBuilder = services
            .AddRefitClient<TRefitClient>(RefitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = apiUrl);

        // Add any delegating handlers.
        if (delegatingHandlerTypes is not null)
        {
            foreach (var type in delegatingHandlerTypes)
            {
                if (!type.IsAssignableTo(typeof(DelegatingHandler)))
                {
                    throw new InvalidOperationException(
                        $"Failed to add delegating handler for '{typeof(TRefitClient).Name}'. '{type.Name}' does not derive from '{nameof(DelegatingHandler)}'.");
                }

                services.AddTransient(type);
                httpClientBuilder.AddHttpMessageHandler(sp => (DelegatingHandler)sp.GetRequiredService(type));
            }
        }

        // Add the Polly policy last to ensure it wraps any previous delegating handlers.
        httpClientBuilder.AddTransientHttpErrorPolicy(
            b => b.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 2)));

        services.AddSingleton<TranslationProviderBase, TTranslationProvider>();
    }
}
