using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Utilities;
using Humanizer;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for the translate message command.
/// </summary>
public partial class TranslateMessageCommandHandler : INotificationHandler<MessageCommandExecutedNotification>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IReadOnlyList<ITranslationProvider> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateMessageCommandHandler" /> class.
    /// </summary>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateMessageCommandHandler(
        IEnumerable<ITranslationProvider> translationProviders,
        IDiscordClient client,
        ILogger<TranslateMessageCommandHandler> logger
    )
    {
        _translationProviders = translationProviders.ToList();
        _client = client;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates the message interacted with to the user's locale.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task Handle(MessageCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Command.Data.Name != MessageCommandConstants.TranslateCommandName)
        {
            return;
        }

        if (notification.Command.Data.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await notification.Command.RespondAsync(
                "Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken }
            );

            return;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(notification.Command.Data.Message.Content);

        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();
            return;
        }

        var userLocale = notification.Command.UserLocale;

        string? providerName = null;
        TranslationResult? translationResult = null;
        foreach (var translationProvider in _translationProviders)
        {
            try
            {
                var targetLanguage = translationProvider.SupportedLanguages.FirstOrDefault(
                    l => l.LangCode == userLocale
                );

                if (targetLanguage == null)
                {
                    var indexOfHyphen = userLocale.IndexOf('-', StringComparison.Ordinal);
                    if (indexOfHyphen > 0)
                    {
                        targetLanguage = translationProvider.SupportedLanguages.FirstOrDefault(
                            l => l.LangCode == userLocale[..indexOfHyphen]
                        );
                    }
                }

                if (targetLanguage == null)
                {
                    _log.UnsupportedLocale(userLocale, translationProvider.ProviderName);
                    continue;
                }

                providerName = translationProvider.ProviderName;

                translationResult = await translationProvider.TranslateAsync(
                    targetLanguage,
                    sanitizedMessage,
                    cancellationToken
                );

                break;
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, translationProvider.GetType());
            }
        }

        if (translationResult == null)
        {
            // Send message if no translation providers support the locale.
            await notification.Command.RespondAsync(
                $"Your locale {userLocale} isn't supported for translation via this action.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken }
            );

            return;
        }

        if (translationResult.TranslatedText == sanitizedMessage)
        {
            _log.FailureToDetectSourceLanguage();

            await notification.Command.RespondAsync(
                "The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken }
            );

            return;
        }

        var fromHeading = $"By {MentionUtils.MentionUser(notification.Command.Data.Message.Author.Id)}";

        if (!string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode))
        {
            fromHeading +=
                $" from {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)}";
        }

        var toHeading =
            $"To {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName})";

        var description =
            $@"{Format.Bold(fromHeading)}:
{sanitizedMessage.Truncate(50)}

{Format.Bold(toHeading)}:
{translationResult.TranslatedText}";

        await notification.Command.RespondAsync(
            embed: new EmbedBuilder()
                .WithTitle("Translated Message")
                .WithUrl(GetJumpUrl(notification.Command.Data.Message).AbsoluteUri)
                .WithDescription(description)
                .Build(),
            ephemeral: true,
            options: new RequestOptions { CancelToken = cancellationToken }
        );
    }

    /// <summary>
    /// Gets the jump URL for a message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The jump URL for the message.</returns>
    public virtual Uri GetJumpUrl(IMessage message)
    {
        return new Uri(message.GetJumpUrl(), UriKind.Absolute);
    }

    private sealed partial class Log
    {
        private readonly ILogger<TranslateMessageCommandHandler> _logger;

        public Log(ILogger<TranslateMessageCommandHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Translating this bot's messages isn't allowed.")]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported locale {locale} for {providerName}.")]
        public partial void UnsupportedLocale(string locale, string providerName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty."
        )]
        public partial void EmptySourceMessage();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerType}.")]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language."
        )]
        public partial void FailureToDetectSourceLanguage();
    }
}
