using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Configuration;

/// <summary>
/// The configuration options for Discord.
/// </summary>
public sealed class DiscordOptions
{
    /// <summary>
    /// Configuration section name for <see cref="DiscordOptions" />.
    /// </summary>
    public const string SectionName = "Discord";

    /// <summary>
    /// The token for the Discord bot.
    /// </summary>
    [Required]
    public required string BotToken { get; init; }
}
