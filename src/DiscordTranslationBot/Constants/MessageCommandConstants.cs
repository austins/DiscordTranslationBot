#pragma warning disable CA1034 // Nested types should not be visible
namespace DiscordTranslationBot.Constants;

/// <summary>
/// Constants for message commands.
/// </summary>
public static class MessageCommandConstants
{
    /// <summary>
    /// "Translate" message command constants.
    /// </summary>
    public static class Translate
    {
        /// <summary>
        /// The name of the "Translate" message command.
        /// </summary>
        public const string CommandName = "Translate";
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
    }
}
