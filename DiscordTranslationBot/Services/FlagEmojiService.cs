using NeoSmart.Unicode;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Provides methods to interact with flag emojis. This should be injected as a singleton so the flag emoji list
/// doesn't have to be regenerated.
/// </summary>
public sealed class FlagEmojiService : IFlagEmojiService
{
    private readonly IEnumerable<SingleEmoji> _emoji;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagEmojiService"/> class.
    /// </summary>
    public FlagEmojiService()
    {
        _emoji = Emoji.All.Where(e => e.Group == "Flags");
    }

    /// <inheritdoc cref="IFlagEmojiService.GetCountryNameBySequence"/>
    public string? GetCountryNameBySequence(UnicodeSequence sequence)
    {
        return _emoji.SingleOrDefault(e => e.Sequence == sequence).Name
            ?.Replace("flag: ", string.Empty, StringComparison.Ordinal);
    }
}
