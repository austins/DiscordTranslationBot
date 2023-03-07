using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Providers.Translation;

namespace DiscordTranslationBot.Extensions;

/// <summary>
/// Extension methods for a service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds options validated with FluentValidation and on start.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configurationSection">The configuration section.</param>
    /// <typeparam name="TOptions">The options type.</typeparam>
    public static void AddOptionsWithFluentValidation<TOptions>(
        this IServiceCollection services,
        IConfigurationSection configurationSection
    ) where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Bind(configurationSection)
            .ValidateWithFluentValidation()
            .ValidateOnStart();
    }

    /// <summary>
    /// Adds translation providers.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns></returns>
    public static IServiceCollection AddTranslationProviders(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Set up configuration.
        var section = configuration.GetSection(TranslationProvidersOptions.SectionName);
        services.AddOptionsWithFluentValidation<TranslationProvidersOptions>(section);

        // Register translation providers. They are prioritized in the order added.
        var options = section.Get<TranslationProvidersOptions>();

        if (options?.AzureTranslator.Enabled == true)
        {
            services.AddSingleton<TranslationProviderBase, AzureTranslatorProvider>();
        }

        if (options?.LibreTranslate.Enabled == true)
        {
            services.AddSingleton<TranslationProviderBase, LibreTranslateProvider>();
        }

        return services;
    }
}
