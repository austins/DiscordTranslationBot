using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for the translate slash command.
/// </summary>
public sealed partial class TranslateSlashCommandHandler : INotificationHandler<SlashCommandExecutedNotification>
{
    private readonly Log _log;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateSlashCommandHandler" /> class.
    /// </summary>
    /// <param name="translationProviders">Translation providers.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateSlashCommandHandler(
        IEnumerable<TranslationProviderBase> translationProviders,
        ILogger<TranslateSlashCommandHandler> logger
    )
    {
        _translationProviders = translationProviders.ToList();
        _log = new Log(logger);
    }

    /// <summary>
    /// Processes the translate slash command interaction.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task Handle(SlashCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Command.Data.Name != SlashCommandConstants.TranslateCommandName)
        {
            return;
        }

        // Get the input values.
        var options = notification.Command.Data.Options;

        var text = (string)options.First(o => o.Name == SlashCommandConstants.TranslateCommandTextOptionName).Value;

        // Parse the input text.
        var sanitizedText = FormatUtility.SanitizeText(text);
        if (string.IsNullOrWhiteSpace(sanitizedText))
        {
            _log.EmptySourceText();

            await notification
                .Command
                .RespondAsync(
                    "No text to translate.",
                    ephemeral: true,
                    options: new RequestOptions { CancelToken = cancellationToken }
                );

            return;
        }

        await notification.Command.DeferAsync(options: new RequestOptions { CancelToken = cancellationToken });

        var to = (string)options.First(o => o.Name == SlashCommandConstants.TranslateCommandToOptionName).Value;

        var from = (string?)
            options.FirstOrDefault(o => o.Name == SlashCommandConstants.TranslateCommandFromOptionName)?.Value;

        // Only the first translation provider is supported as the slash command options were registered with one provider's supported languages.
        var translationProvider = _translationProviders[0];

        try
        {
            var sourceLanguage =
                from != null ? translationProvider.SupportedLanguages.First(l => l.LangCode == from) : null;

            var targetLanguage = translationProvider.SupportedLanguages.First(l => l.LangCode == to);

            var translationResult = await translationProvider.TranslateAsync(
                targetLanguage,
                sanitizedText,
                cancellationToken,
                sourceLanguage
            );

            if (translationResult.TranslatedText == sanitizedText)
            {
                _log.FailureToDetectSourceLanguage();

                await notification
                    .Command
                    .FollowupAsync(
                        "Couldn't detect the source language to translate from or the result is the same.",
                        options: new RequestOptions { CancelToken = cancellationToken }
                    );

                return;
            }

            await notification
                .Command
                .FollowupAsync(
                    $"""
                 {MentionUtils.MentionUser(notification.Command.User.Id)} translated text using {translationProvider.ProviderName} from {Format.Italics(sourceLanguage?.Name ?? translationResult.DetectedLanguageName)}:
                 {Format.Quote(sanitizedText)}
                 To {Format.Italics(translationResult.TargetLanguageName)}:
                 {Format.Quote(translationResult.TranslatedText)}
                 """,
                    options: new RequestOptions { CancelToken = cancellationToken }
                );
        }
        catch (Exception ex)
        {
            _log.TranslationFailure(ex, translationProvider.GetType());
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<TranslateSlashCommandHandler> _logger;

        public Log(ILogger<TranslateSlashCommandHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty."
        )]
        public partial void EmptySourceText();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerType}.")]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language."
        )]
        public partial void FailureToDetectSourceLanguage();
    }
}
