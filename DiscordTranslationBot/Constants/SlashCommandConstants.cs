#pragma warning disable CA1034 // Nested types should not be visible
namespace DiscordTranslationBot.Constants;

/// <summary>
/// Constants for slash commands.
/// </summary>
public static class SlashCommandConstants
{
    /// <summary>
    /// Translate slash command constants.
    /// </summary>
    public static class Translate
    {
        /// <summary>
        /// The name of the translate slash command.
        /// </summary>
        public const string CommandName = "translate";

        /// <summary>
        /// The to option name of the translate slash command.
        /// </summary>
        public const string CommandToOptionName = "to";

        /// <summary>
        /// The text option name of the translate slash command.
        /// </summary>
        public const string CommandTextOptionName = "text";

        /// <summary>
        /// The from option name of the translate slash command.
        /// </summary>
        public const string CommandFromOptionName = "from";
    }
}
