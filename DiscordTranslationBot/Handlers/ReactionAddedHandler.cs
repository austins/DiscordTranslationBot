using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using MediatR;
using NeoSmart.Unicode;

using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handles the ReactionAdded event of the Discord client.
/// </summary>
internal sealed class ReactionAddedHandler : INotificationHandler<ReactionAddedNotification>
{
    private readonly IEnumerable<ITranslationProvider> _translationProviders;
    private readonly DiscordSocketClient _client;
    private readonly FlagEmojiService _flagEmojiService;
    private readonly ILogger<ReactionAddedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionAddedHandler"/> class.
    /// </summary>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="flagEmojiService">FlagEmojiService to use.</param>
    /// <param name="logger">Logger to use.</param>
    public ReactionAddedHandler(
        IEnumerable<ITranslationProvider> translationProviders,
        DiscordSocketClient client,
        FlagEmojiService flagEmojiService,
        ILogger<ReactionAddedHandler> logger)
    {
        _translationProviders = translationProviders;
        _client = client;
        _flagEmojiService = flagEmojiService;
        _logger = logger;
    }

    /// <summary>
    /// Translates any message that got a flag emoji reaction on it.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Reaction == null || !Emoji.IsEmoji(notification.Reaction.Emote.Name)) return;

        var countryName =
            _flagEmojiService.GetCountryNameBySequence(notification.Reaction.Emote.Name.AsUnicodeSequence());

        if (countryName == null) return;

        var sourceMessage = await notification.Message.GetOrDownloadAsync();

        if (sourceMessage.Author.Id == _client.CurrentUser.Id)
        {
            _logger.LogInformation("Translating this bot's messages isn't allowed.");
            await sourceMessage.RemoveReactionAsync(notification.Reaction.Emote, notification.Reaction.UserId);
            return;
        }

        // Remove all user and channel mentions and custom emotes,
        // then strip all markdown to make the translation clean.
        var sanitizedMessage = Format.StripMarkDown(
            Regex.Replace(sourceMessage.Content, @"<(?::\w+:|@!*&*|#)[0-9]+>", string.Empty));

        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _logger.LogInformation("Nothing to translate. The sanitized source message is empty.");
            await sourceMessage.RemoveReactionAsync(notification.Reaction.Emote, notification.Reaction.UserId);
            return;
        }

        try
        {
            TranslationResult? translationResult = null;
            foreach (var translationProvider in _translationProviders)
            {
                try
                {
                    translationResult = await translationProvider.TranslateAsync(countryName, sanitizedMessage, cancellationToken);
                    break;
                }
                catch (UnsupportedCountryException ex) when (translationProvider is LibreTranslateProvider)
                {
                    SendTempMessage(
                        ex.Message,
                        notification.Reaction,
                        sourceMessage,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to translate text with {translationProvider.GetType()}.");
                }
            }

            if (translationResult == null) return;

            string replyText;
            if (translationResult.TranslatedText == sanitizedMessage)
            {
                _logger.LogWarning(
                    "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.");

                replyText = "Couldn't detect the source language to translate from or the result is the same.";
            }
            else
            {
                replyText = !string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode) ?
                    $"Translated message from {Format.Italics(translationResult.DetectedLanguageCode)} to {Format.Italics(translationResult.TargetLanguageCode)} ({translationResult.ProviderName}):\n{Format.BlockQuote(translationResult.TranslatedText)}" :
                    $"Translated message to {Format.Italics(translationResult.TargetLanguageCode)} ({translationResult.ProviderName}):\n{Format.BlockQuote(translationResult.TranslatedText)}";
            }

            SendTempMessage(replyText, notification.Reaction, sourceMessage, cancellationToken);
        }
        catch (HttpRequestException ex) when
            (ex.StackTrace?.Contains(nameof(LibreTranslate.Net.LibreTranslate), StringComparison.Ordinal) == true)
        {
            _logger.LogError(ex, "Unable to connect to the LibreTranslate API URL.");
        }
    }

    /// <summary>
    /// Sends a message and then clears the reaction and message after a certain time.
    /// </summary>
    /// <param name="text">Text to send in message.</param>
    /// <param name="reaction">The reaction.</param>
    /// <param name="sourceMessage">The source message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static void SendTempMessage(
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
    }
}