using System.Text.RegularExpressions;
using Discord;

namespace DiscordTranslationBot.Utilities;

/// <summary>
/// Utility for formatting.
/// </summary>
public static partial class FormatUtility
{
    /// <summary>
    /// Remove all user and channel mentions and custom emotes,
    /// then strip all markdown to make the translation clean.
    /// </summary>
    /// <param name="text">Text to sanitize.</param>
    /// <returns>Sanitized text.</returns>
    public static string SanitizeText(string text)
    {
        return Format.StripMarkDown(DiscordSyntaxRegex().Replace(text, string.Empty)).Trim();
    }

    [GeneratedRegex(@"<((@!?&?\d+)|(a?:.+?:\d+))>")]
    private static partial Regex DiscordSyntaxRegex();
}
