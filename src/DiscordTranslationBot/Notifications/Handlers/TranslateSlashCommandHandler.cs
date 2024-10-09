using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Notifications.Handlers;

internal sealed partial class TranslateSlashCommandHandler : INotificationHandler<SlashCommandExecutedNotification>
{
    private readonly Log _log;
    private readonly ITranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateSlashCommandHandler" /> class.
    /// </summary>
    /// <param name="translationProviderFactory">Translation provider factory.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateSlashCommandHandler(
        ITranslationProviderFactory translationProviderFactory,
        ILogger<TranslateSlashCommandHandler> logger)
    {
        _translationProviderFactory = translationProviderFactory;
        _log = new Log(logger);
    }

    /// <summary>
    /// Processes the translate slash command interaction.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask Handle(SlashCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Interaction.Data.Name != SlashCommandConstants.Translate.CommandName)
        {
            return;
        }

        // Get the input values.
        var options = notification.Interaction.Data.Options;

        var text = (string)options.First(o => o.Name == SlashCommandConstants.Translate.CommandTextOptionName).Value;

        // Parse the input text.
        var sanitizedText = FormatUtility.SanitizeText(text);
        if (string.IsNullOrWhiteSpace(sanitizedText))
        {
            _log.EmptySourceText();

            await notification.Interaction.RespondAsync(
                "No text to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        await notification.Interaction.DeferAsync(options: new RequestOptions { CancelToken = cancellationToken });

        var to = (string)options.First(o => o.Name == SlashCommandConstants.Translate.CommandToOptionName).Value;

        var from = (string?)options.FirstOrDefault(o => o.Name == SlashCommandConstants.Translate.CommandFromOptionName)
            ?.Value;

        // Only the first translation provider is supported as the slash command options were registered with one provider's supported languages.
        var translationProvider = _translationProviderFactory.PrimaryProvider;

        try
        {
            var sourceLanguage = from is not null
                ? translationProvider.SupportedLanguages.First(l => l.LangCode == from)
                : null;

            var targetLanguage = translationProvider.SupportedLanguages.First(l => l.LangCode == to);

            var translationResult = await translationProvider.TranslateAsync(
                targetLanguage,
                sanitizedText,
                cancellationToken,
                sourceLanguage);

            if (translationResult.TranslatedText == sanitizedText)
            {
                _log.FailureToDetectSourceLanguage();

                await notification.Interaction.FollowupAsync(
                    "Couldn't detect the source language to translate from or the result is the same.",
                    options: new RequestOptions { CancelToken = cancellationToken });

                return;
            }

            await notification.Interaction.FollowupAsync(
                $"""
                 {MentionUtils.MentionUser(notification.Interaction.User.Id)} translated text from {Format.Italics(sourceLanguage?.Name ?? translationResult.DetectedLanguageName)}:
                 {Format.Quote(sanitizedText)}
                 To {Format.Italics(translationResult.TargetLanguageName)}:
                 {Format.Quote(translationResult.TranslatedText)}
                 """,
                options: new RequestOptions { CancelToken = cancellationToken });
        }
        catch (Exception ex)
        {
            _log.TranslationFailure(ex, translationProvider.GetType());
        }
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceText();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerType}.")]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}
