using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.AzureTranslator;

public sealed class Translation
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
