using NeoSmart.Unicode;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Interface for FlagEmojiService.
/// </summary>
public interface IFlagEmojiService
{
    /// <summary>
    /// Get a country name by the Unicode sequence of an emoji.
    /// </summary>
    /// <param name="sequence">The Unicode sequence.</param>
    /// <returns>Country name.</returns>
    string? GetCountryNameBySequence(UnicodeSequence sequence);
}
