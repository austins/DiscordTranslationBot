using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

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
    /// <param name="requestBody">Request body to serialize.</param>
    /// <returns>StringContent to set assigned to <see cref="HttpRequestMessage.Content"/>.</returns>
    public static StringContent SerializeTranslationRequestContent(this HttpClient httpClient, object requestBody)
    {
        var serializedObject = JsonSerializer.Serialize(requestBody, SerializerOptions);

        return new StringContent(serializedObject, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Deserializes response content to a type suitable for processing a translation result.
    /// </summary>
    /// <param name="httpContent">The HttpContent instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">Type to deserialize response content to.</typeparam>
    /// <returns>Deserialized response content.</returns>
    public static Task<T?> DeserializeTranslationResponseContentAsync<T>(
        this HttpContent httpContent,
        CancellationToken cancellationToken)
    {
        return httpContent.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
    }
}
