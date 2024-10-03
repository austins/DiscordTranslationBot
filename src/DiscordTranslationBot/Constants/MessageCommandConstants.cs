#pragma warning disable CA1034 // Nested types should not be visible
namespace DiscordTranslationBot.Constants;

/// <summary>
/// Constants for message commands.
/// </summary>
public static class MessageCommandConstants
{
    /// <summary>
    /// "Translate (Auto)" message command constants.
    /// </summary>
    public static class TranslateAuto
    {
        /// <summary>
        /// The name of the "Translate (Auto)" message command.
        /// </summary>
        public const string CommandName = "Translate (Auto)";
    }

    /// <summary>
    /// "Translate To..." message command constants.
    /// </summary>
    public static class TranslateTo
    {
        /// <summary>
        /// The name of the "Translate To..." message command.
        /// </summary>
        public const string CommandName = "Translate To...";

        /// <summary>
        /// The unique custom ID of the select menu.
        /// </summary>
        public static readonly string SelectMenuId = $"{nameof(TranslateTo)}_SelectMenu";

        /// <summary>
        /// The unique custom ID of the translate button.
        /// </summary>
        public static readonly string TranslateButtonId = $"{nameof(TranslateTo)}_TranslateButton";

        /// <summary>
        /// The unique custom ID of the translate and share button.
        /// </summary>
        public static readonly string TranslateAndShareButtonId = $"{nameof(TranslateTo)}_TranslateAndShareButton";
    }
}
