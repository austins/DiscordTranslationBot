using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Services;
using LibreTranslate.Net;
using MediatR;
using NeoSmart.Unicode;
using System.Text.RegularExpressions;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Handlers;

/// <summary>Handles the ReactionAdded event of the Discord client.</summary>
internal sealed class ReactionAddedHandler : INotificationHandler<ReactionAddedNotification>
{
    private readonly FlagEmojiService _flagEmojiService;
    private readonly LibreTranslate.Net.LibreTranslate _libreTranslate;
    private readonly ILogger<ReactionAddedHandler> _logger;

    /// <summary>Initializes the handler.</summary>
    /// <param name="libreTranslate">LibreTranslate client to use.</param>
    /// <param name="flagEmojiService">FlagEmojiService to use.</param>
    /// <param name="logger">Logger to use.</param>
    public ReactionAddedHandler(
        LibreTranslate.Net.LibreTranslate libreTranslate,
        FlagEmojiService flagEmojiService,
        ILogger<ReactionAddedHandler> logger)
    {
        _libreTranslate = libreTranslate;
        _flagEmojiService = flagEmojiService;
        _logger = logger;
    }

    /// <summary>Translates any message that got a flag emoji reaction on it.</summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Reaction == null || !Emoji.IsEmoji(notification.Reaction.Emote.Name)) return;

        var countryName =
            _flagEmojiService.GetCountryNameBySequence(notification.Reaction.Emote.Name.AsUnicodeSequence());

        if (countryName == null) return;

        var sourceMessage = await notification.Message.GetOrDownloadAsync();

        var targetLangCode = FlagEmojiService.GetLanguageCodeByCountryName(countryName);
        if (targetLangCode == null)
        {
            _logger.LogInformation($"Translation for country [{countryName}] isn't supported.");

            await SendTempMessage(
                $"Translation for country {countryName} isn't supported.",
                notification.Reaction,
                sourceMessage,
                cancellationToken);

            return;
        }

        // Remove all user and channel mentions and custom emotes,
        // then strip all markdown to make the translation clean.
        var sanitizedMessage = Format.StripMarkDown(
            Regex.Replace(sourceMessage.Content, @"<(?::\w+:|@!*&*|#)[0-9]+>", string.Empty));

        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _logger.LogInformation("Nothing to translate. The sanitized source message is empty.");
            return;
        }

        try
        {
            var translatedText = await _libreTranslate.TranslateAsync(
                new Translate { Source = LanguageCode.AutoDetect, Target = targetLangCode, Text = sanitizedMessage });

            string replyText;
            if (translatedText == sanitizedMessage)
            {
                _logger.LogWarning(
                    "Couldn't detect the source language to translate from. This could happen when the LibreTranslate detected language confidence is 0.");

                replyText = "Couldn't detect the source language to translate from.";
            }
            else
            {
                replyText =
                    $"Translated message to {Format.Italics(targetLangCode.ToString())}:\n{Format.BlockQuote(translatedText)}";
            }

            await SendTempMessage(replyText, notification.Reaction, sourceMessage, cancellationToken);
        }
        catch (HttpRequestException ex) when
            (ex.StackTrace?.Contains(nameof(LibreTranslate.Net.LibreTranslate)) == true)
        {
            _logger.LogError(ex, "Unable to connect to the LibreTranslate API URL.");
        }
    }

    /// <summary>Sends a message and then clears the reaction and message after a certain time.</summary>
    /// <param name="text">Text to send in message.</param>
    /// <param name="reaction">The reaction.</param>
    /// <param name="sourceMessage">The source message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static Task SendTempMessage(
        string text,
        SocketReaction reaction,
        IMessage sourceMessage,
        CancellationToken cancellationToken)
    {
        // Wrapped in Task.Run to not block the handler as the cleanup has a delay of over 3 seconds.
        _ = Task.Run(
            async () =>
            {
                // Send message.
                var replyMessage = await sourceMessage.Channel.SendMessageAsync(
                    text,
                    messageReference: new MessageReference(sourceMessage.Id));

                // Cleanup.
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                await sourceMessage.RemoveReactionAsync(reaction.Emote, reaction.UserId);
                await replyMessage.DeleteAsync();
            },
            cancellationToken);

        return Task.CompletedTask;
    }
}