using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Commands.Translation;

public sealed class TranslateBySlashCommand : ICommand
{
    /// <summary>
    /// The slash command.
    /// </summary>
    [Required]
    public required ISlashCommandInteraction SlashCommand { get; init; }
}

public sealed partial class TranslateBySlashCommandHandler
    : ICommandHandler<TranslateBySlashCommand>,
        INotificationHandler<SlashCommandExecutedEvent>
{
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly TranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateBySlashCommandHandler" /> class.
    /// </summary>
    /// <param name="translationProviderFactory">Translation provider factory.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateBySlashCommandHandler(
        TranslationProviderFactory translationProviderFactory,
        IMediator mediator,
        ILogger<TranslateBySlashCommandHandler> logger)
    {
        _translationProviderFactory = translationProviderFactory;
        _mediator = mediator;
        _log = new Log(logger);
    }

    /// <summary>
    /// Processes the translate slash command interaction.
    /// </summary>
    /// <param name="command">The Mediator command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask<Unit> Handle(TranslateBySlashCommand command, CancellationToken cancellationToken)
    {
        // Get the input values.
        var options = command.SlashCommand.Data.Options;

        var text = (string)options.First(o => o.Name == SlashCommandConstants.Translate.CommandTextOptionName).Value;

        // Parse the input text.
        var sanitizedText = FormatUtility.SanitizeText(text);
        if (string.IsNullOrWhiteSpace(sanitizedText))
        {
            _log.EmptySourceText();

            await command.SlashCommand.RespondAsync(
                "No text to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return Unit.Value;
        }

        await command.SlashCommand.DeferAsync(options: new RequestOptions { CancelToken = cancellationToken });

        var to = (string)options.First(o => o.Name == SlashCommandConstants.Translate.CommandToOptionName).Value;

        var from = (string?)options.FirstOrDefault(o => o.Name == SlashCommandConstants.Translate.CommandFromOptionName)
            ?.Value;

        // Only the first translation provider is supported as the slash command options were registered with one provider's supported languages.
        try
        {
            var sourceLanguage = from is not null
                ? _translationProviderFactory.PrimaryProvider.SupportedLanguages.First(l => l.LangCode == from)
                : null;

            var targetLanguage =
                _translationProviderFactory.PrimaryProvider.SupportedLanguages.First(l => l.LangCode == to);

            var translationResult = await _translationProviderFactory.PrimaryProvider.TranslateAsync(
                targetLanguage,
                sanitizedText,
                cancellationToken,
                sourceLanguage);

            if (translationResult.TranslatedText == sanitizedText)
            {
                _log.FailureToDetectSourceLanguage();

                await command.SlashCommand.FollowupAsync(
                    "Couldn't detect the source language to translate from or the result is the same.",
                    options: new RequestOptions { CancelToken = cancellationToken });

                return Unit.Value;
            }

            await command.SlashCommand.FollowupAsync(
                $"""
                 {MentionUtils.MentionUser(command.SlashCommand.User.Id)} translated text using {_translationProviderFactory.PrimaryProvider.ProviderName} from {Format.Italics(sourceLanguage?.Name ?? translationResult.DetectedLanguageName)}:
                 {Format.Quote(sanitizedText)}
                 To {Format.Italics(translationResult.TargetLanguageName)}:
                 {Format.Quote(translationResult.TranslatedText)}
                 """,
                options: new RequestOptions { CancelToken = cancellationToken });
        }
        catch (Exception ex)
        {
            _log.TranslationFailure(ex, _translationProviderFactory.PrimaryProvider.GetType());
        }

        return Unit.Value;
    }

    public async ValueTask Handle(SlashCommandExecutedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.SlashCommand.Data.Name != SlashCommandConstants.Translate.CommandName)
        {
            return;
        }

        await _mediator.Send(
            new TranslateBySlashCommand { SlashCommand = notification.SlashCommand },
            cancellationToken);
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
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
