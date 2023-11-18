using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using DiscordTranslationBot.Models.Providers.Translation;

namespace DiscordTranslationBot.Extensions;

/// <summary>
/// Extension methods for serializing and deserializing translation requests and results.
/// </summary>
public static class TranslationSerializationExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    /// <summary>
    /// Deserializes response content to a type suitable for processing a translation result.
    /// </summary>
    /// <param name="content">The HttpContent instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TTranslateResult">Type to deserialize response content to.</typeparam>
    /// <returns>Deserialized response content.</returns>
    public static Task<TTranslateResult?> ReadAsTranslateResultAsync<TTranslateResult>(
        this HttpContent content,
        CancellationToken cancellationToken)
        where TTranslateResult : ITranslateResult, new()
    {
        return content.ReadFromJsonAsync<TTranslateResult>(SerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Deserializes response content list to a type suitable for processing a translation result.
    /// </summary>
    /// <param name="content">The HttpContent instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TTranslateResult">Type to deserialize response content to.</typeparam>
    /// <returns>Deserialized response content.</returns>
    public static Task<IList<TTranslateResult>?> ReadAsTranslateResultsAsync<TTranslateResult>(
        this HttpContent content,
        CancellationToken cancellationToken)
        where TTranslateResult : ITranslateResult, new()
    {
        return content.ReadFromJsonAsync<IList<TTranslateResult>>(SerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Serializes a request body object to be used in a request for a translation.
    /// </summary>
    /// <param name="request">Translate request to serialize.</param>
    /// <returns>StringContent to set assigned to <see cref="HttpRequestMessage.Content" />.</returns>
    public static StringContent SerializeToRequestContent(this ITranslateRequest request)
    {
        return new StringContent(
            JsonSerializer.Serialize(request, SerializerOptions),
            Encoding.UTF8,
            "application/json");
    }

    /// <summary>
    /// Serializes a list of request body objects to be used in a request for a translation.
    /// </summary>
    /// <param name="request">List of translate requests to serialize.</param>
    /// <returns>StringContent to set assigned to <see cref="HttpRequestMessage.Content" />.</returns>
    public static StringContent SerializeToRequestContent(this IList<ITranslateRequest> request)
    {
        return new StringContent(
            JsonSerializer.Serialize(request, SerializerOptions),
            Encoding.UTF8,
            "application/json");
    }
}
