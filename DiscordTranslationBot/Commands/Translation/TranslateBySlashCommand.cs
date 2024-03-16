using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Commands.Translation;

public sealed class TranslateBySlashCommand : IRequest
{
    /// <summary>
    /// The slash command.
    /// </summary>
    public required ISlashCommandInteraction SlashCommand { get; init; }
}

public sealed partial class TranslateBySlashCommandHandler
    : IRequestHandler<TranslateBySlashCommand>, INotificationHandler<SlashCommandExecutedEvent>
{
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateBySlashCommandHandler" /> class.
    /// </summary>
    /// <param name="translationProviders">Translation providers.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateBySlashCommandHandler(
        IEnumerable<TranslationProviderBase> translationProviders,
        IMediator mediator,
        ILogger<TranslateBySlashCommandHandler> logger)
    {
        _translationProviders = translationProviders.ToList();
        _mediator = mediator;
        _log = new Log(logger);
    }

    public Task Handle(SlashCommandExecutedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.SlashCommand.Data.Name != SlashCommandConstants.TranslateCommandName)
        {
            return Task.CompletedTask;
        }

        return _mediator.Send(
            new TranslateBySlashCommand { SlashCommand = notification.SlashCommand },
            cancellationToken);
    }

    /// <summary>
    /// Processes the translate slash command interaction.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task Handle(TranslateBySlashCommand request, CancellationToken cancellationToken)
    {
        // Get the input values.
        var options = request.SlashCommand.Data.Options;

        var text = (string)options.First(o => o.Name == SlashCommandConstants.TranslateCommandTextOptionName).Value;

        // Parse the input text.
        var sanitizedText = FormatUtility.SanitizeText(text);
        if (string.IsNullOrWhiteSpace(sanitizedText))
        {
            _log.EmptySourceText();

            await request.SlashCommand.RespondAsync(
                "No text to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        await request.SlashCommand.DeferAsync(options: new RequestOptions { CancelToken = cancellationToken });

        var to = (string)options.First(o => o.Name == SlashCommandConstants.TranslateCommandToOptionName).Value;

        var from = (string?)options.FirstOrDefault(o => o.Name == SlashCommandConstants.TranslateCommandFromOptionName)
            ?.Value;

        // Only the first translation provider is supported as the slash command options were registered with one provider's supported languages.
        var translationProvider = _translationProviders[0];

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

                await request.SlashCommand.FollowupAsync(
                    "Couldn't detect the source language to translate from or the result is the same.",
                    options: new RequestOptions { CancelToken = cancellationToken });

                return;
            }

            await request.SlashCommand.FollowupAsync(
                $"""
                 {MentionUtils.MentionUser(request.SlashCommand.User.Id)} translated text using {translationProvider.ProviderName} from {Format.Italics(sourceLanguage?.Name ?? translationResult.DetectedLanguageName)}:
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

    private sealed partial class Log
    {
        private readonly ILogger<TranslateBySlashCommandHandler> _logger;

        public Log(ILogger<TranslateBySlashCommandHandler> logger)
        {
            _logger = logger;
        }

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
