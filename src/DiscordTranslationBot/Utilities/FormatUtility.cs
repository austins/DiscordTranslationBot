using Markdig;
using System.Text;
using System.Text.RegularExpressions;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Utilities;

/// <summary>
/// Utility for formatting.
/// </summary>
internal static partial class FormatUtility
{
    /// <summary>
    /// Regex for all user, channel mentions, and custom emotes.
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"<((@!?&?\d+)|(a?:.+?:\d+))>")]
    private static partial Regex DiscordSyntaxRegex { get; }

    /// <summary>
    /// Regex for all Markdown fenced code blocks.
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"\`\`\`(?:.|[\r\n])*?\`\`\`")]
    private static partial Regex MarkdownFencedCodeBlockRegex { get; }

    /// <summary>
    /// Regex for all Markdown links.
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex MarkdownLinkRegex { get; }

    /// <summary>
    /// Regex for all URLs starting with "http://" or "https://".
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"https?:\/\/\S+")]
    private static partial Regex UrlRegex { get; }

    /// <summary>
    /// Remove special Discord syntax, emojis, and Markdown to only translate what is necessary
    /// and reduce translation providers' character quota usage.
    /// </summary>
    /// <param name="text">Text to sanitize.</param>
    /// <returns>Sanitized text.</returns>
    public static string SanitizeText(string text)
    {
        // Remove all user mentions, channel mentions, and custom Discord emoji.
        var result = DiscordSyntaxRegex.Replace(text, string.Empty);

        // Remove text within all Markdown fenced code blocks so the text contained gets removed instead of getting converted to plain text.
        result = MarkdownFencedCodeBlockRegex.Replace(result, string.Empty);

        // Remove Markdown links first so its text gets removed instead of getting converted to plain text.
        result = MarkdownLinkRegex.Replace(result, string.Empty);

        // Convert all remaining Markdown to plain text.
        result = Markdown.ToPlainText(result);

        // Remove URLs starting with "http://" or "https://".
        result = UrlRegex.Replace(result, string.Empty);

        // Remove all unicode emoji. Emoji are checked per code point, so surrogate pairs are grouped
        // into a single unit before the check.
        var builder = new StringBuilder(result.Length);
        Span<char> surrogateUnit = stackalloc char[2];
        var index = 0;
        while (index < result.Length)
        {
            var c = result[index];
            if (char.IsHighSurrogate(c) && index + 1 < result.Length && char.IsLowSurrogate(result[index + 1]))
            {
                surrogateUnit[0] = c;
                surrogateUnit[1] = result[index + 1];
                if (!Emoji.IsEmoji(new string(surrogateUnit)))
                {
                    builder.Append(c).Append(result[index + 1]);
                }

                index++;
            }
            else if (char.IsSurrogate(c) || !Emoji.IsEmoji(c.ToString()))
            {
                // Lone surrogates are preserved because they are not valid code points to check.
                builder.Append(c);
            }

            index++;
        }

        result = builder.ToString();

        // Trim and return sanitized text.
        return result.Trim();
    }
}
