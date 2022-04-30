using System.Text.RegularExpressions;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Services;
using LibreTranslate.Net;
using MediatR;
using NeoSmart.Unicode;
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

        // Wrap the long calls in Task.Run and to allow the calls after delay to not lock the handler.
        _ = Task.Run(
            async () =>
            {
                try
                {
                    bool doTranslation = true;
                    IUserMessage? replyMessage = null;

                    var channel = await notification.Channel.GetOrDownloadAsync();

                    var targetLangCode = FlagEmojiService.GetLanguageCodeByCountryName(countryName);
                    if (targetLangCode == null)
                    {
                        _logger.LogInformation($"Translation for country [{countryName}] isn't supported.");

                        replyMessage = await channel.SendMessageAsync(
                            $"Translation for country {countryName} isn't supported.",
                            messageReference: new MessageReference(sourceMessage.Id));

                        doTranslation = false;
                    }

                    // Remove all user and channel mentions and custom emotes,
                    // then strip all markdown to make the translation clean.
                    var sanitizedMessage = Format.StripMarkDown(
                        Regex.Replace(sourceMessage.Content, @"<(?::\w+:|@!*&*|#)[0-9]+>", string.Empty));

                    if (string.IsNullOrWhiteSpace(sanitizedMessage))
                    {
                        _logger.LogInformation("Nothing to translate. The sanitized source message is empty.");
                        doTranslation = false;
                    }

                    if (doTranslation)
                    {
                        var translatedText = await _libreTranslate.TranslateAsync(
                            new Translate
                            {
                                Source = LanguageCode.AutoDetect, Target = targetLangCode, Text = sanitizedMessage
                            });

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

                        replyMessage = await channel.SendMessageAsync(
                            replyText,
                            messageReference: new MessageReference(sourceMessage.Id));
                    }

                    // Cleanup
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                    await sourceMessage.RemoveReactionAsync(notification.Reaction.Emote, notification.Reaction.UserId);

                    if (replyMessage != null)
                    {
                        await replyMessage.DeleteAsync();
                    }
                }
                catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions)
                {
                    _logger.LogError(ex, "Missing permissions for channel.");
                }
                catch (HttpRequestException ex) when (ex.StackTrace?.Contains(nameof(LibreTranslate.Net.LibreTranslate)) == true)
                {
                    _logger.LogError(ex, "Unable to connect to the LibreTranslate API URL.");
                }
            },
            cancellationToken);
    }
}