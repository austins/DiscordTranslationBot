using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using DiscordTranslationBot.Discord.Models;
using IMessage = Discord.IMessage;

namespace DiscordTranslationBot.Discord.Services;

public sealed partial class MessageHelper : IMessageHelper
{
    public Uri GetJumpUrl(IMessage message)
    {
        return new Uri(message.GetJumpUrl(), UriKind.Absolute);
    }

    public IReadOnlyList<JumpUrl> GetJumpUrlsInMessage(IMessage message)
    {
        var jumpUrls = new List<JumpUrl>();
        foreach (var groups in JumpUrlRegex().Matches(message.CleanContent).Select(m => m.Groups))
        {
            var isDmChannel = groups[0].Value == "@me";

            jumpUrls.Add(
                new JumpUrl
                {
                    IsDmChannel = isDmChannel,
                    GuildId = isDmChannel ? null : ulong.Parse(groups[1].Value, CultureInfo.InvariantCulture),
                    ChannelId = ulong.Parse(groups[2].Value, CultureInfo.InvariantCulture),
                    MessageId = ulong.Parse(groups[3].Value, CultureInfo.InvariantCulture)
                });
        }

        return jumpUrls;
    }

    [GeneratedRegex(@"https:\/\/discord\.com\/channels\/(@me|\d+)\/(\d+)\/(\d+)")]
    private static partial Regex JumpUrlRegex();
}

public interface IMessageHelper
{
    public Uri GetJumpUrl(IMessage message);

    public IReadOnlyList<JumpUrl> GetJumpUrlsInMessage(IMessage message);
}
