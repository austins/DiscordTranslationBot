using DiscordTranslationBot.Configuration.TranslationProviders;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot.Providers.Translation.AzureTranslator;

internal sealed class AzureTranslatorHeadersHandler : DelegatingHandler
{
    private readonly AzureTranslatorOptions _azureTranslatorOptions;

    public AzureTranslatorHeadersHandler(IOptions<TranslationProvidersOptions> translationProvidersOptions)
    {
        _azureTranslatorOptions = translationProvidersOptions.Value.AzureTranslator;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("Ocp-Apim-Subscription-Key", _azureTranslatorOptions.SecretKey);
        request.Headers.Add("Ocp-Apim-Subscription-Region", _azureTranslatorOptions.Region);

        return base.SendAsync(request, cancellationToken);
    }
}
