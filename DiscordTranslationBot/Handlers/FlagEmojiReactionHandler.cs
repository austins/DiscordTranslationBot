using AsyncAwaitBestPractices;
using Discord;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using DiscordTranslationBot.Utilities;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for flag emoji reactions.
/// </summary>
public partial class FlagEmojiReactionHandler : INotificationHandler<ReactionAddedNotification>
{
    private readonly IDiscordClient _client;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;
    private readonly ICountryService _countryService;
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagEmojiReactionHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="countryService">Country service to use.</param>
    /// <param name="logger">Logger to use.</param>
    public FlagEmojiReactionHandler(
        IDiscordClient client,
        IEnumerable<TranslationProviderBase> translationProviders,
        ICountryService countryService,
        ILogger<FlagEmojiReactionHandler> logger
    )
    {
        _client = client;
        _translationProviders = translationProviders.ToList();
        _countryService = countryService;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates any message that got a flag emoji reaction on it.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        if (
            !Emoji.IsEmoji(notification.Reaction.Emote.Name)
            || !_countryService.TryGetCountry(notification.Reaction.Emote.Name, out var country)
        )
        {
            return;
        }

        if (notification.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await notification
                .Message
                .RemoveReactionAsync(
                    notification.Reaction.Emote,
                    notification.Reaction.UserId,
                    new RequestOptions { CancelToken = cancellationToken }
                );

            return;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(notification.Message.Content);

        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await notification
                .Message
                .RemoveReactionAsync(
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

                translationResult = await translationProvider.TranslateByCountryAsync(
                    country,
                    sanitizedMessage,
                    cancellationToken
                );

                break;
            }
            catch (UnsupportedCountryException ex)
            {
                _log.UnsupportedCountry(ex, country.Name, translationProvider.GetType());

                // Send message if this is the last available translation provider.
                if (translationProvider == _translationProviders[^1])
                {
                    SendTempReply(
                        ex.Message,
                        notification.Reaction,
                        notification.Message,
                        cancellationToken
                    );

                    return;
                }
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, translationProvider.GetType());
            }
        }

        if (translationResult == null)
        {
            await notification
                .Message
                .RemoveReactionAsync(
                    notification.Reaction.Emote,
                    notification.Reaction.UserId,
                    new RequestOptions { CancelToken = cancellationToken }
                );

            return;
        }

        if (translationResult.TranslatedText == sanitizedMessage)
        {
            _log.FailureToDetectSourceLanguage();

            SendTempReply(
                "Couldn't detect the source language to translate from or the result is the same.",
                notification.Reaction,
                notification.Message,
                cancellationToken
            );

            return;
        }

        // Send the reply message.
        var replyText = !string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode)
            ? $"""
               Translated message from {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)} to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):
               {Format.BlockQuote(translationResult.TranslatedText)}
               """
            : $"""
               Translated message to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):
               {Format.BlockQuote(translationResult.TranslatedText)}
               """;

        SendTempReply(
            replyText,
            notification.Reaction,
            notification.Message,
            cancellationToken,
            20
        );
    }

    /// <summary>
    /// Sends a reply and then clears the reaction and reply after a certain time.
    /// </summary>
    /// <param name="text">Text to send in message.</param>
    /// <param name="reaction">The reaction.</param>
    /// <param name="message">The referenced message to reply to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="deletionDelayInSeconds">How many seconds the message should be shown.</param>
    public virtual void SendTempReply(
        string text,
        Reaction reaction,
        IUserMessage message,
        CancellationToken cancellationToken,
        uint deletionDelayInSeconds = 10
    )
    {
        HandleSendTempMessage().SafeFireAndForget(ex => _log.FailedToSendTempMessage(ex, message.Id, text));
        return;

        async Task HandleSendTempMessage()
        {
            // Send reply message.
            var reply = await message.Channel.SendMessageAsync(
                text,
                messageReference: new MessageReference(message.Id),
                options: new RequestOptions { CancelToken = cancellationToken }
            );

            // Cleanup.
            await Task.Delay(TimeSpan.FromSeconds(deletionDelayInSeconds), cancellationToken);

            // If the source message still exists, remove the reaction from it.
            var sourceMessage = await reply
                .Channel
                .GetMessageAsync(message.Id, options: new RequestOptions { CancelToken = cancellationToken });

            if (sourceMessage != null)
            {
                await sourceMessage.RemoveReactionAsync(
                    reaction.Emote,
                    reaction.UserId,
                    new RequestOptions { CancelToken = cancellationToken }
                );
            }

            // Delete the reply message.
            await reply.DeleteAsync(new RequestOptions { CancelToken = cancellationToken });
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<FlagEmojiReactionHandler> _logger;

        public Log(ILogger<FlagEmojiReactionHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Translating this bot's messages isn't allowed.")]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty."
        )]
        public partial void EmptySourceMessage();

        [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported country {countryName} for {providerType}.")]
        public partial void UnsupportedCountry(Exception ex, string? countryName, Type providerType);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerType}.")]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language."
        )]
        public partial void FailureToDetectSourceLanguage();

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to send temp message for reaction to message ID {referencedMessageId} with text: {text}"
        )]
        public partial void FailedToSendTempMessage(Exception ex, ulong referencedMessageId, string text);
    }
}
