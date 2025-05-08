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
public sealed partial class TranslateAutoMessageCommandHandler
    : INotificationHandler<MessageCommandExecutedNotification>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMessageHelper _messageHelper;
    private readonly ITranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateAutoMessageCommandHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviderFactory">Translation provider factory to use.</param>
    /// <param name="messageHelper">Message helper to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateAutoMessageCommandHandler(
        IDiscordClient client,
        ITranslationProviderFactory translationProviderFactory,
        IMessageHelper messageHelper,
        ILogger<TranslateAutoMessageCommandHandler> logger)
    {
        _client = client;
        _translationProviderFactory = translationProviderFactory;
        _messageHelper = messageHelper;
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
                "Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(notification.Interaction.Data.Message.Content);
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await notification.Interaction.RespondAsync(
                "No text to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        await notification.Interaction.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

        var userLocale = notification.Interaction.UserLocale;

        TranslationResult? translationResult = null;
        foreach (var translationProvider in _translationProviderFactory.Providers)
        {
            var providerName = translationProvider.GetType().Name;
            _log.TranslatorAttempt(providerName);

            try
            {
                var targetLanguage =
                    translationProvider.SupportedLanguages.FirstOrDefault(l => l.LangCode == userLocale);

                if (targetLanguage is null)
                {
                    var indexOfHyphen = userLocale.IndexOf('-', StringComparison.Ordinal);
                    if (indexOfHyphen > 0)
                    {
                        targetLanguage =
                            translationProvider.SupportedLanguages.FirstOrDefault(l =>
                                l.LangCode == userLocale[..indexOfHyphen]);
                    }
                }

                if (targetLanguage is null)
                {
                    _log.UnsupportedLocale(userLocale, providerName);
                    continue;
                }

                translationResult = await translationProvider.TranslateAsync(
                    targetLanguage,
                    sanitizedMessage,
                    cancellationToken);

                _log.TranslationSuccess(providerName);

                break;
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, providerName);
            }
        }

        if (translationResult is null)
        {
            // Send message if no translation providers support the locale.
            await notification.Interaction.FollowupAsync(
                $"Your locale {userLocale} isn't supported for translation via this action.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        if (translationResult.TranslatedText == sanitizedMessage)
        {
            _log.FailureToDetectSourceLanguage();

            await notification.Interaction.FollowupAsync(
                "The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        await notification.Interaction.FollowupAsync(
            _messageHelper.BuildTranslationReplyWithReference(notification.Interaction.Data.Message, translationResult),
            ephemeral: true,
            options: new RequestOptions { CancelToken = cancellationToken });
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Translating this bot's messages isn't allowed.")]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported locale {locale} for {providerName}.")]
        public partial void UnsupportedLocale(string locale, string providerName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceMessage();

        [LoggerMessage(Level = LogLevel.Information, Message = "Attempting to use {providerName}...")]
        public partial void TranslatorAttempt(string providerName);

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully translated text with {providerName}.")]
        public partial void TranslationSuccess(string providerName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerName}.")]
        public partial void TranslationFailure(Exception ex, string providerName);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}
