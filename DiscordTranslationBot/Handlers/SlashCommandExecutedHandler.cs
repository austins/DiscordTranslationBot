using Discord;
using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Utilities;
using Mediator;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for the slash command executed event.
/// </summary>
public sealed partial class SlashCommandExecutedHandler
    : INotificationHandler<SlashCommandExecutedNotification>,
        ICommandHandler<ProcessTranslateCommand>
{
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<ITranslationProvider> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashCommandExecutedHandler"/> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="translationProviders">Translation providers.</param>
    /// <param name="logger">Logger to use.</param>
    public SlashCommandExecutedHandler(
        IMediator mediator,
        IEnumerable<ITranslationProvider> translationProviders,
        ILogger<SlashCommandExecutedHandler> logger
    )
    {
        _mediator = mediator;
        _translationProviders = translationProviders.ToList();
        _log = new Log(logger);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="command">The Mediator command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask<Unit> Handle(
        ProcessTranslateCommand command,
        CancellationToken cancellationToken
    )
    {
        // Get the input values.
        var options = command.Command.Data.Options;

        var from = (string)
            options.First(o => o.Name == CommandConstants.TranslateCommandFromOptionName).Value;

        var to = (string)
            options.First(o => o.Name == CommandConstants.TranslateCommandToOptionName).Value;

        var text = (string)
            options.First(o => o.Name == CommandConstants.TranslateCommandTextOptionName).Value;

        // Parse the input text.
        var sanitizedText = FormatUtility.SanitizeText(text);
        if (string.IsNullOrWhiteSpace(sanitizedText))
        {
            _log.EmptySourceText();
            await command.Command.RespondAsync("Nothing to translate.", ephemeral: true);
            return Unit.Value;
        }

        var translationProvider = _translationProviders[0];

        try
        {
            var sourceLanguage =
                from != CommandConstants.TranslateCommandFromOptionAutoValue
                    ? translationProvider.SupportedLanguages.First(l => l.LangCode == from)
                    : null;

            var targetLanguage = translationProvider.SupportedLanguages.First(
                l => l.LangCode == to
            );

            var translationResult = await translationProvider.TranslateAsync(
                targetLanguage,
                sanitizedText,
                cancellationToken,
                sourceLanguage
            );

            if (translationResult.TranslatedText == sanitizedText)
            {
                _log.FailureToDetectSourceLanguage();

                await command.Command.RespondAsync(
                    "Couldn't detect the source language to translate from or the result is the same.",
                    ephemeral: true
                );

                return Unit.Value;
            }

            await command.Command.RespondAsync(
                $@"Translated text using {translationProvider.ProviderName} from {Format.Italics(sourceLanguage?.Name ?? translationResult.DetectedLanguageName)}:
{Format.Quote(sanitizedText)}
To {Format.Italics(translationResult.TargetLanguageName)}:
{Format.Quote(translationResult.TranslatedText)}"
            );
        }
        catch (Exception ex)
        {
            _log.TranslationFailure(ex, translationProvider.GetType());
        }

        return Unit.Value;
    }

    /// <summary>
    /// Delegates slash command executed events to the right handler.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask Handle(
        SlashCommandExecutedNotification notification,
        CancellationToken cancellationToken
    )
    {
        if (notification.Command.Data.Name == CommandConstants.TranslateCommandName)
        {
            await _mediator.Send(
                new ProcessTranslateCommand { Command = notification.Command },
                cancellationToken
            );
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<SlashCommandExecutedHandler> _logger;

        public Log(ILogger<SlashCommandExecutedHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty."
        )]
        public partial void EmptySourceText();

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to translate text with {providerType}."
        )]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language."
        )]
        public partial void FailureToDetectSourceLanguage();
    }
}
