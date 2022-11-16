using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using Mediator;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handles the ReactionAdded event of the Discord client for flag emotes.
/// </summary>
public sealed class FlagReactionAddedHandler : INotificationHandler<ReactionAddedNotification>
{
    private readonly DiscordSocketClient _client;
    private readonly ICountryService _countryService;
    private readonly ILogger<FlagReactionAddedHandler> _logger;
    private readonly IEnumerable<TranslationProviderBase> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagReactionAddedHandler"/> class.
    /// </summary>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="countryService">Country service to use.</param>
    /// <param name="logger">Logger to use.</param>
    public FlagReactionAddedHandler(
        IEnumerable<TranslationProviderBase> translationProviders,
        DiscordSocketClient client,
        ICountryService countryService,
        ILogger<FlagReactionAddedHandler> logger
    )
    {
        _translationProviders = translationProviders;
        _client = client;
        _countryService = countryService;
        _logger = logger;
    }

    /// <summary>
    /// Translates any message that got a flag emoji reaction on it.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask Handle(
        ReactionAddedNotification notification,
        CancellationToken cancellationToken
    )
    {
        if (
            !Emoji.IsEmoji(notification.Reaction.Emote.Name)
            || !_countryService.TryGetCountry(notification.Reaction.Emote.Name, out var country)
        )
        {
            return;
        }

        var sourceMessage = await notification.Message;

        if (sourceMessage.Author.Id == _client.CurrentUser?.Id)
        {
            _logger.LogInformation("Translating this bot's messages isn't allowed.");

            await sourceMessage.RemoveReactionAsync(
                notification.Reaction.Emote,
                notification.Reaction.UserId,
                new RequestOptions { CancelToken = cancellationToken }
            );

            return;
        }

        // Remove all user and channel mentions and custom emotes,
        // then strip all markdown to make the translation clean.
        var sanitizedMessage = Format.StripMarkDown(
            Regex.Replace(sourceMessage.Content, @"<(?::\w+:|@!*&*|#)[0-9]+>", string.Empty)
        );

        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _logger.LogInformation("Nothing to translate. The sanitized source message is empty.");

            await sourceMessage.RemoveReactionAsync(
                notification.Reaction.Emote,
                notification.Reaction.UserId,
                new RequestOptions { CancelToken = cancellationToken }
            );

            return;
        }

        string? providerName = null;
        TranslationResult? translationResult = null;
        foreach (var translationProvider in _translationProviders)
        {
            try
            {
                providerName = translationProvider.ProviderName;

                translationResult = await translationProvider.TranslateAsync(
                    country!,
                    sanitizedMessage,
                    cancellationToken
                );

                break;
            }
            catch (UnsupportedCountryException ex)
                when (translationProvider is LibreTranslateProvider)
            {
                SendTempMessage(
                    ex.Message,
                    notification.Reaction,
                    sourceMessage.Channel,
                    sourceMessage.Id,
                    cancellationToken
                );

                return;
            }
            catch (UnsupportedCountryException ex)
            {
                _logger.LogWarning(
                    ex,
                    $"Unsupported country {country?.Name} for {translationProvider.GetType()}."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"Failed to translate text with {translationProvider.GetType()}."
                );
            }
        }

        if (providerName == null || translationResult == null)
        {
            await sourceMessage.RemoveReactionAsync(
                notification.Reaction.Emote,
                notification.Reaction.UserId,
                new RequestOptions { CancelToken = cancellationToken }
            );

            return;
        }

        if (translationResult.TranslatedText == sanitizedMessage)
        {
            _logger.LogWarning(
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language."
            );

            SendTempMessage(
                "Couldn't detect the source language to translate from or the result is the same.",
                notification.Reaction,
                sourceMessage.Channel,
                sourceMessage.Id,
                cancellationToken
            );

            return;
        }

        // Send the reply message.
        var replyText = !string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode)
            ? $"Translated message from {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)} to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):\n{Format.BlockQuote(translationResult.TranslatedText)}"
            : $"Translated message to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):\n{Format.BlockQuote(translationResult.TranslatedText)}";

        SendTempMessage(
            replyText,
            notification.Reaction,
            sourceMessage.Channel,
            sourceMessage.Id,
            cancellationToken,
            20
        );
    }

    /// <summary>
    /// Sends a message and then clears the reaction and message after a certain time.
    /// </summary>
    /// <param name="text">Text to send in message.</param>
    /// <param name="reaction">The reaction.</param>
    /// <param name="channel">The channel to post the message in.</param>
    /// <param name="referencedMessageId">The source message ID to reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="seconds">How many seconds the message should be shown.</param>
    private static void SendTempMessage(
        string text,
        Reaction reaction,
        IMessageChannel channel,
        ulong referencedMessageId,
        CancellationToken cancellationToken,
        uint seconds = 10
    )
    {
        using var typingState = channel.EnterTypingState();

        // Wrapped in Task.Run to not block the handler as the cleanup has a delay of over 3 seconds.
        _ = Task.Run(
            async () =>
            {
                // Send reply message.
                var replyMessage = await channel.SendMessageAsync(
                    text,
                    messageReference: new MessageReference(referencedMessageId),
                    options: new RequestOptions { CancelToken = cancellationToken }
                );

                // Cleanup.
                await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);

                // If the source message still exists, remove the reaction from it.
                var sourceMessage = await replyMessage.Channel.GetMessageAsync(
                    referencedMessageId,
                    options: new RequestOptions { CancelToken = cancellationToken }
                );

                if (sourceMessage != null)
                {
                    await sourceMessage.RemoveReactionAsync(
                        reaction.Emote,
                        reaction.UserId,
                        new RequestOptions { CancelToken = cancellationToken }
                    );
                }

                // Delete the reply message.
                await replyMessage.DeleteAsync(
                    new RequestOptions { CancelToken = cancellationToken }
                );
            },
            cancellationToken
        );
    }
}
