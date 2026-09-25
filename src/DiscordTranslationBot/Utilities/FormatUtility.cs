using Markdig;
using System.Globalization;
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
    /// Regex for all user and role mentions, and custom emotes.
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
    /// Regex for Discord tokens containing an ID or Unix time, such as channel mentions, slash command mentions,
    /// and timestamps.
    /// </summary>
    /// <returns>Regex.</returns>
    [GeneratedRegex(@"<[^<>]*?(\d{5,})[^<>]*>")]
    private static partial Regex DiscordTokenRegex { get; }

    /// <summary>
    /// Restores Discord tokens in translated text to their original form from the source text.
    /// Translators can alter tokens, such as changing the case of a timestamp's style, which prevents Discord from
    /// rendering them.
    /// </summary>
    /// <param name="sourceText">The text that was translated.</param>
    /// <param name="translatedText">The translated text.</param>
    /// <returns>Translated text with the original Discord tokens.</returns>
    public static string RestoreDiscordTokens(string sourceText, string translatedText)
    {
        // ponytail: tokens are matched by ID, so the same ID with different styles restores to the first style.
        var originalTokens = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in DiscordTokenRegex.Matches(sourceText))
        {
            originalTokens.TryAdd(match.Groups[1].Value, match.Value);
        }

        return originalTokens.Count == 0
            ? translatedText
            : DiscordTokenRegex.Replace(
                translatedText,
                match => originalTokens.GetValueOrDefault(match.Groups[1].Value, match.Value));
    }

    /// <summary>
    /// Remove special Discord syntax, emojis, and Markdown to only translate what is necessary
    /// and reduce translation providers' character quota usage.
    /// </summary>
    /// <param name="text">Text to sanitize.</param>
    /// <returns>Sanitized text.</returns>
    public static string SanitizeText(string text)
    {
        // Remove all user mentions, role mentions, and custom Discord emoji.
        var result = DiscordSyntaxRegex.Replace(text, string.Empty);

        // Remove text within all Markdown fenced code blocks so the text contained gets removed instead of getting converted to plain text.
        result = MarkdownFencedCodeBlockRegex.Replace(result, string.Empty);

        // Remove Markdown links first so its text gets removed instead of getting converted to plain text.
        result = MarkdownLinkRegex.Replace(result, string.Empty);

        // Convert all remaining Markdown to plain text.
        result = Markdown.ToPlainText(result);

        // Remove URLs starting with "http://" or "https://".
        result = UrlRegex.Replace(result, string.Empty);

        // Remove unicode emoji recognized by Unicode.net. Text is checked per grapheme cluster so
        // multi-code point emoji (ZWJ sequences, skin tones, variation selectors, keycaps) are removed whole.
        var stringBuilder = new StringBuilder(result.Length);
        var index = 0;
        while (index < result.Length)
        {
            var length = StringInfo.GetNextTextElementLength(result.AsSpan(index));
            var element = result.AsSpan(index, length);

            // A lone ASCII char is never an emoji, so skip the costly check. Lone surrogates are
            // preserved because they are not valid code points to check.
            if ((length == 1 && (char.IsAscii(element[0]) || char.IsSurrogate(element[0])))
                || !Emoji.IsEmoji(element.ToString()))
            {
                stringBuilder.Append(element);
            }

            index += length;
        }

        result = stringBuilder.ToString();

        // Trim and return sanitized text.
        return result.Trim();
    }
}
