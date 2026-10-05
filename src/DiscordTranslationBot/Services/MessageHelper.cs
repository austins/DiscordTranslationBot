using Discord;
using DiscordTranslationBot.Providers.Translation.Models;
using System.Text;
using IMessage = Discord.IMessage;

namespace DiscordTranslationBot.Services;

internal sealed class MessageHelper : IMessageHelper
{
    public string BuildTranslationReplyWithReference(
        IMessage referencedMessage,
        TranslationResult translationResult,
        ulong? interactionUserId = null)
    {
        var stringBuilder =
            new StringBuilder(interactionUserId is null ? "You" : MentionUtils.MentionUser(interactionUserId.Value))
                .Append(" translated ")
                .Append(referencedMessage.GetJumpUrl());

        if (interactionUserId != referencedMessage.Author.Id)
        {
            stringBuilder.Append(" by ").Append(MentionUtils.MentionUser(referencedMessage.Author.Id));
        }

        if (!string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode))
        {
            stringBuilder
                .Append(" from ")
                .Append(
                    Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode));
        }

        return stringBuilder
            .Append(" to ")
            .Append(Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode))
            .Append(":\n")
            .Append(Format.BlockQuote(translationResult.TranslatedText))
            .ToString();
    }
}

internal interface IMessageHelper
{
    /// <summary>
    /// Build a reply for a message being translated with a jump URL and info about the referenced message.
    /// </summary>
    /// <remarks>
    /// This is useful in cases of ephemeral messages, which cannot have a message reference,
    /// but messages sent directly to a channel can.
    /// </remarks>
    /// <param name="referencedMessage">The message being translated.</param>
    /// <param name="translationResult">The translation result.</param>
    /// <param name="interactionUserId">Optionally, the user who invoked the interaction, which will be mentioned.</param>
    /// <returns>Content text with jump URL and info of message being translated.</returns>
    public string BuildTranslationReplyWithReference(
        IMessage referencedMessage,
        TranslationResult translationResult,
        ulong? interactionUserId = null);
}
