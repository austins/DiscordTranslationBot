#pragma warning disable CA1034 // Nested types should not be visible
namespace DiscordTranslationBot.Constants;

/// <summary>
/// Constants for message commands.
/// </summary>
internal static class MessageCommandConstants
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
        /// The custom ID prefix of the modal, followed by the ID of the message to translate.
        /// </summary>
        public const string ModalIdPrefix = $"{nameof(TranslateTo)}_Modal:";

        /// <summary>
        /// The unique custom ID of the language select menu in the modal.
        /// </summary>
        public const string LanguageSelectMenuId = $"{nameof(TranslateTo)}_LanguageSelectMenu";

        /// <summary>
        /// The unique custom ID of the share checkbox in the modal.
        /// </summary>
        public const string ShareCheckboxId = $"{nameof(TranslateTo)}_ShareCheckbox";
    }
}
