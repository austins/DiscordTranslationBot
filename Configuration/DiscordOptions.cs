namespace DiscordTranslationBot.Configuration;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    /// <summary>The token for the Discord bot.</summary>
    public string? BotToken { get; set; }
}