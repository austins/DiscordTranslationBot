using FluentValidation;

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
    public required string BotToken { get; init; }
}

/// <summary>
/// Validator for <see cref="DiscordOptions" />.
/// </summary>
public sealed class DiscordOptionsValidator : AbstractValidator<DiscordOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordOptionsValidator" /> class.
    /// </summary>
    public DiscordOptionsValidator()
    {
        RuleFor(x => x.BotToken).NotEmpty();
    }
}
