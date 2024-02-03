using System.Text;
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

        // Remove text within all Markdown fenced code blocks.
        result = MarkdownFencedCodeBlockRegex().Replace(result, string.Empty);

        // Remove Markdown links first so its text gets removed instead of getting converted to plain text.
        result = MarkdownLinkRegex().Replace(result, string.Empty);

        // Remove Markdown.
        result = Markdown.ToPlainText(result);

        // Remove URLs starting with "http://" or "https://".
        result = UrlRegex().Replace(result, string.Empty);

        // Remove all unicode emoji.
        var stringBuilder = new StringBuilder();
        foreach (var letter in result.Letters().Where(letter => !Emoji.IsEmoji(letter)))
        {
            stringBuilder.Append(letter);
        }

        result = stringBuilder.ToString();

        // Trim and return sanitized text.
        return result.Trim();
    }

    /// <summary>
    /// Regex for all user, channel mentions, and custom emotes.
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"<((@!?&?\d+)|(a?:.+?:\d+))>")]
    private static partial Regex DiscordSyntaxRegex();

    [GeneratedRegex(@"\`\`\`(?:.|[\r\n])*?\`\`\`")]
    private static partial Regex MarkdownFencedCodeBlockRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"\b(?:https?://)\S+\b")]
    private static partial Regex UrlRegex();
}
