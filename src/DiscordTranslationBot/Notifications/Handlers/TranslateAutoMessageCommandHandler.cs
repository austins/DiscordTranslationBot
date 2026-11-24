using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;
using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Notifications.Handlers;

/// <summary>
/// Handler for the "Translate (Auto)" message command.
/// </summary>
internal sealed partial class TranslateAutoMessageCommandHandler
    : INotificationHandler<MessageCommandExecutedNotification>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMessageHelper _messageHelper;
    private readonly ITranslationRateLimiter _rateLimiter;
    private readonly ITranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateAutoMessageCommandHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviderFactory">Translation provider factory to use.</param>
    /// <param name="messageHelper">Message helper to use.</param>
    /// <param name="rateLimiter">Translation rate limiter to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateAutoMessageCommandHandler(
        IDiscordClient client,
        ITranslationProviderFactory translationProviderFactory,
        IMessageHelper messageHelper,
        ITranslationRateLimiter rateLimiter,
        ILogger<TranslateAutoMessageCommandHandler> logger)
    {
        _client = client;
        _translationProviderFactory = translationProviderFactory;
        _messageHelper = messageHelper;
        _rateLimiter = rateLimiter;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates the message interacted with to the user's locale.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask Handle(MessageCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Interaction.Data.Name != MessageCommandConstants.TranslateAuto.CommandName)
        {
            return;
        }

        if (notification.Interaction.Data.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await notification.Interaction.RespondAsync(
                $"{NeoSmart.Unicode.Emoji.NoEntry} Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(notification.Interaction.Data.Message.Content);
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await notification.Interaction.RespondAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} No text to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        if (!_rateLimiter.TryAcquire(notification.Interaction.User.Id))
        {
            await notification.Interaction.RespondAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} {ITranslationRateLimiter.RateLimitedMessage}",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        await notification.Interaction.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

        var userLocale = notification.Interaction.UserLocale;

        TranslationOutcome outcome;
        try
        {
            outcome = await _translationProviderFactory.TranslateAsync(
                async (translationProvider, ct) =>
                {
                    SupportedLanguage? targetLanguage =
                        translationProvider.SupportedLanguages.TryGetValue(userLocale, out var name)
                            ? new SupportedLanguage(userLocale, name)
                            : null;

                    // Check for fallback lang code (e.g. en-US -> en).
                    if (targetLanguage is null)
                    {
                        var indexOfHyphen = userLocale.IndexOf('-', StringComparison.Ordinal);
                        if (indexOfHyphen > 0)
                        {
                            var fallbackLangCode = userLocale[..indexOfHyphen];

                            if (translationProvider.SupportedLanguages.TryGetValue(
                                    fallbackLangCode,
                                    out var fallbackName))
                            {
                                targetLanguage = new SupportedLanguage(fallbackLangCode, fallbackName);
                            }
                        }
                    }

                    if (targetLanguage is null)
                    {
                        var providerName = translationProvider.GetType().Name;
                        _log.UnsupportedLocale(userLocale, providerName);
                        return new UnsupportedTargetLanguage(providerName);
                    }

                    return await translationProvider.TranslateAsync(targetLanguage.Value, sanitizedMessage, ct);
                },
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

#pragma warning disable CA1508 // The analyzer does not track union case patterns.
        var reply = outcome switch
        {
            TranslationResult when sameText =>
                $"{NeoSmart.Unicode.Emoji.Warning} The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
            TranslationResult result => _messageHelper.BuildTranslationReplyWithReference(
                notification.Interaction.Data.Message,
                result),
            UnsupportedTargetLanguage =>
                $"{NeoSmart.Unicode.Emoji.NoEntry} Your locale {userLocale} isn't supported for translation via this action.",
            ProviderFailure => $"{NeoSmart.Unicode.Emoji.Warning} Failed to translate text. Please try again.",
            null => throw new InvalidOperationException("The translation factory returned an empty outcome.")
        };
#pragma warning restore CA1508

        await notification.Interaction.FollowupAsync(
            reply,
            ephemeral: true,
            options: new RequestOptions { CancelToken = cancellationToken });
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Translating this bot's messages isn't allowed.")]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text.")]
        public partial void TranslationFailure(Exception ex);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported locale {locale} for {providerName}.")]
        public partial void UnsupportedLocale(string locale, string providerName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceMessage();

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}
