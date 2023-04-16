using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using DiscordTranslationBot.Models.Providers.Translation;

namespace DiscordTranslationBot.Extensions;

/// <summary>
/// Extensions methods for translation operations.
/// </summary>
public static class TranslationExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    /// <summary>
    /// Serializes a request body object to be used in a request for a translation.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <param name="request">Translate request to serialize.</param>
    /// <returns>StringContent to set assigned to <see cref="HttpRequestMessage.Content" />.</returns>
    public static StringContent SerializeTranslationRequestContent<TTranslateRequest>(
        this HttpClient httpClient,
        TTranslateRequest request) where TTranslateRequest : ITranslateRequest
    {
        return new StringContent(
            JsonSerializer.Serialize(request, SerializerOptions),
            Encoding.UTF8,
            "application/json");
    }

    /// <summary>
    /// Serializes a list of request body objects to be used in a request for a translation.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <param name="request">List of translate requests to serialize.</param>
    /// <returns>StringContent to set assigned to <see cref="HttpRequestMessage.Content" />.</returns>
    public static StringContent SerializeTranslationRequestContent<TTranslateRequest>(
        this HttpClient httpClient,
        IList<TTranslateRequest> request) where TTranslateRequest : ITranslateRequest
    {
        return new StringContent(
            JsonSerializer.Serialize(request, SerializerOptions),
            Encoding.UTF8,
            "application/json");
    }

    /// <summary>
    /// Deserializes response content to a type suitable for processing a translation result.
    /// </summary>
    /// <param name="httpContent">The HttpContent instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TTranslateResult">Type to deserialize response content to.</typeparam>
    /// <returns>Deserialized response content.</returns>
    public static Task<TTranslateResult?> DeserializeTranslationResponseContentAsync<TTranslateResult>(
        this HttpContent httpContent,
        CancellationToken cancellationToken) where TTranslateResult : ITranslateResult
    {
        return httpContent.ReadFromJsonAsync<TTranslateResult>(SerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Deserializes response content list to a type suitable for processing a translation result.
    /// </summary>
    /// <param name="httpContent">The HttpContent instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TTranslateResult">Type to deserialize response content to.</typeparam>
    /// <returns>Deserialized response content.</returns>
    public static Task<IList<TTranslateResult>?> DeserializeTranslationResponseContentsAsync<TTranslateResult>(
        this HttpContent httpContent,
        CancellationToken cancellationToken) where TTranslateResult : ITranslateResult
    {
        return httpContent.ReadFromJsonAsync<IList<TTranslateResult>>(SerializerOptions, cancellationToken);
    }
}
