using System.Text.RegularExpressions;
using Markdig;
using NeoSmart.Unicode;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Utilities;

/// <summary>
/// Utility for formatting.
/// </summary>
public static partial class FormatUtility
{
    /// <summary>
    /// Remove special Discord syntax, emojis, and Markdown to only translate what is necessary
    /// and reduce translation providers' character quota usage.
    /// </summary>
    /// <param name="text">Text to sanitize.</param>
    /// <returns>Sanitized text.</returns>
    public static string SanitizeText(string text)
    {
        // Remove all user mentions, channel mentions, and custom Discord emoji.
        var result = DiscordSyntaxRegex().Replace(text, string.Empty);

        // Remove text within all Markdown fenced code blocks so the text contained gets removed instead of getting converted to plain text.
        result = MarkdownFencedCodeBlockRegex().Replace(result, string.Empty);

        // Remove Markdown links first so its text gets removed instead of getting converted to plain text.
        result = MarkdownLinkRegex().Replace(result, string.Empty);

        // Convert all remaining Markdown to plain text.
        result = Markdown.ToPlainText(result);

        // Remove URLs starting with "http://" or "https://".
        result = UrlRegex().Replace(result, string.Empty);

        // Remove all unicode emoji.
        result = string.Concat(result.Letters().Where(letter => !Emoji.IsEmoji(letter)));

        // Trim and return sanitized text.
        return result.Trim();
    }

    /// <summary>
    /// Regex for all user, channel mentions, and custom emotes.
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"<((@!?&?\d+)|(a?:.+?:\d+))>")]
    private static partial Regex DiscordSyntaxRegex();

    /// <summary>
    /// Regex for all Markdown fenced code blocks.
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"\`\`\`(?:.|[\r\n])*?\`\`\`")]
    private static partial Regex MarkdownFencedCodeBlockRegex();

    /// <summary>
    /// Regex for all Markdown links.
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex MarkdownLinkRegex();

    /// <summary>
    /// Regex for all URLs starting with "http://" or "https://".
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"https?:\/\/\S+")]
    private static partial Regex UrlRegex();
}
