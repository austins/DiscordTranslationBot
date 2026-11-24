using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Countries;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;
using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Notifications.Handlers;

/// <summary>
/// Handler for translating by a flag emoji reaction.
/// </summary>
internal sealed partial class TranslateByCountryFlagEmojiReactionHandler
    : INotificationHandler<ReactionAddedNotification>
{
    private static readonly TimeSpan TempReplyDeletionDelay = TimeSpan.FromMinutes(1.5);
    private static readonly TimeSpan FailureReplyDeletionDelay = TimeSpan.FromSeconds(15);

    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMessageHelper _messageHelper;
    private readonly ITranslationRateLimiter _rateLimiter;
    private readonly ISender _sender;
    private readonly ITranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateByCountryFlagEmojiReactionHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviderFactory">Translation provider factory to use.</param>
    /// <param name="sender">Mediator sender to use.</param>
    /// <param name="messageHelper">Message helper to use.</param>
    /// <param name="rateLimiter">Translation rate limiter to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateByCountryFlagEmojiReactionHandler(
        IDiscordClient client,
        ITranslationProviderFactory translationProviderFactory,
        ISender sender,
        IMessageHelper messageHelper,
        ITranslationRateLimiter rateLimiter,
        ILogger<TranslateByCountryFlagEmojiReactionHandler> logger)
    {
        _client = client;
        _translationProviderFactory = translationProviderFactory;
        _sender = sender;
        _messageHelper = messageHelper;
        _rateLimiter = rateLimiter;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates any message that got a country flag emoji reaction on it.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.ReactionInfo.IsBot)
        {
            return;
        }

        if (!CountryConstants.SupportedCountries.TryGetValue(notification.ReactionInfo.Emote.Name, out var country))
        {
            return;
        }

        if (notification.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await notification.Message.RemoveReactionAsync(
                notification.ReactionInfo.Emote,
                notification.ReactionInfo.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(notification.Message.Content);
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await notification.Message.RemoveReactionAsync(
                notification.ReactionInfo.Emote,
                notification.ReactionInfo.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        // Silently ignore rate limited reactions to avoid making additional Discord API calls.
        if (!_rateLimiter.TryAcquire(notification.ReactionInfo.UserId))
        {
            return;
        }

        TranslationOutcome outcome;
        try
        {
            outcome = await _translationProviderFactory.TranslateAsync(
                (translationProvider, ct) => translationProvider.TranslateByCountryAsync(country, sanitizedMessage, ct),
                cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _log.TranslationFailure(ex);
            outcome = new ProviderFailure("Translation failed.", Cause: ex);
        }

        var sameText = outcome.Value is TranslationResult { TranslatedText: var translatedText }
            && translatedText == sanitizedMessage;

        if (sameText)
        {
            _log.FailureToDetectSourceLanguage();
        }

        if (outcome.Value is UnsupportedTargetLanguage unsupported)
        {
            _log.LanguageNotSupportedForCountry(unsupported.ProviderName, country.Name);
        }

#pragma warning disable CA1508 // The analyzer does not track union case patterns.
        var (text, deletionDelay) = outcome switch
        {
            TranslationResult when sameText => (
                $"{NeoSmart.Unicode.Emoji.Warning} Couldn't detect the source language to translate from or the result is the same.",
                FailureReplyDeletionDelay),
            TranslationResult result => (
                _messageHelper.BuildTranslationReplyWithReference(
                    notification.Message,
                    result,
                    notification.ReactionInfo.UserId),
                TempReplyDeletionDelay),
            UnsupportedTargetLanguage(var providerName) => (
                $"{NeoSmart.Unicode.Emoji.NoEntry} Target language isn't supported by {providerName} for {country.Name}.",
                FailureReplyDeletionDelay),
            ProviderFailure => (
                $"{NeoSmart.Unicode.Emoji.Warning} Failed to translate text. Please try again.",
                FailureReplyDeletionDelay),
            null => throw new InvalidOperationException("The translation factory returned an empty outcome.")
        };
#pragma warning restore CA1508

        // Reactions can't receive ephemeral replies, so send a temporary message instead.
        await _sender.Send(
            new SendTempReply
            {
                Text = text,
                ReactionInfo = notification.ReactionInfo,
                SourceMessage = notification.Message,
                DeletionDelay = deletionDelay
            },
            cancellationToken);
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Translating this bot's messages isn't allowed.")]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceMessage();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text.")]
        public partial void TranslationFailure(Exception ex);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Target language code not supported. Provider {providerName} doesn't support the language code or the country {countryName} has no mapping for the language code.")]
        public partial void LanguageNotSupportedForCountry(string providerName, string countryName);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}
