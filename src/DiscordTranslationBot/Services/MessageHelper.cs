﻿using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Providers.Translation.Models;
using IMessage = Discord.IMessage;

namespace DiscordTranslationBot.Services;

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

    public string BuildTranslationReplyWithReference(
        IMessage referencedMessage,
        TranslationResult translationResult,
        ulong? interactionUserId = null)
    {
        var replyText =
            $"{(interactionUserId is null ? "You" : MentionUtils.MentionUser(interactionUserId.Value))} translated {GetJumpUrl(referencedMessage)} by {MentionUtils.MentionUser(referencedMessage.Author.Id)}";

        if (!string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode))
        {
            replyText +=
                $" from {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)}";
        }

        replyText +=
            $" to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)}:\n{Format.BlockQuote(translationResult.TranslatedText)}";

        return replyText;
    }

    [GeneratedRegex(@"https:\/\/discord\.com\/channels\/(@me|\d+)\/(\d+)\/(\d+)")]
    private static partial Regex JumpUrlRegex();
}

public interface IMessageHelper
{
    /// <summary>
    /// Get a jump URL for a message.
    /// </summary>
    /// <remarks>
    /// Discord.Net's <see cref="MessageExtensions.GetJumpUrl" /> is an extension. In order to test it, we must wrap it.
    /// </remarks>
    /// <param name="message">The message to get a jump URL for.</param>
    /// <returns>Jump URL of the message.</returns>
    public Uri GetJumpUrl(IMessage message);

    public IReadOnlyList<JumpUrl> GetJumpUrlsInMessage(IMessage message);

    /// <summary>
    /// Build a reply for a message being translated.
    /// </summary>
    /// <remarks>
    /// Ephemeral messages cannot have a message reference, but messages sent directly to a channel can, we can have
    /// a jump URL and info about the referenced message in the content text.
    /// </remarks>
    /// <param name="referencedMessage">The message being translated.</param>
    /// <param name="translationResult">The translation result.</param>
    /// <param name="interactionUserId">Optionally, the user who invoked the interaction.</param>
    /// <returns>Content text with jump URL and info of message being translated.</returns>
    public string BuildTranslationReplyWithReference(
        IMessage referencedMessage,
        TranslationResult translationResult,
        ulong? interactionUserId = null);
}
