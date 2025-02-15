namespace DiscordTranslationBot.Discord;

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
    public required string BotToken { get; init; }
}

public sealed class DiscordOptionsValidator : AbstractValidator<DiscordOptions>
{
    public DiscordOptionsValidator()
    {
        RuleFor(x => x.BotToken).NotEmpty();
    }
}
