using System.Text.Json;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Utilities;
using Humanizer;
using Mediator;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for the slash command executed event.
/// </summary>
public sealed partial class SlashCommandExecutedHandler
    : INotificationHandler<SlashCommandExecutedNotification>,
        ICommandHandler<RegisterSlashCommands>,
        ICommandHandler<ProcessTranslateSlashCommand>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<ITranslationProvider> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashCommandExecutedHandler" /> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="translationProviders">Translation providers.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="logger">Logger to use.</param>
    public SlashCommandExecutedHandler(
        IMediator mediator,
        IEnumerable<ITranslationProvider> translationProviders,
        IDiscordClient client,
        ILogger<SlashCommandExecutedHandler> logger)
    {
        _mediator = mediator;
        _translationProviders = translationProviders.ToList();
        _client = client;
        _log = new Log(logger);
    }

    /// <summary>
    /// </summary>
    /// <param name="command">The Mediator command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask<Unit> Handle(ProcessTranslateSlashCommand command, CancellationToken cancellationToken)
    {
        // Get the input values.
        var options = command.Command.Data.Options;

        var to = (string)options.First(o => o.Name == SlashCommandConstants.TranslateCommandToOptionName).Value;

        var text = (string)options.First(o => o.Name == SlashCommandConstants.TranslateCommandTextOptionName).Value;

        var from = (string?)options.FirstOrDefault(o => o.Name == SlashCommandConstants.TranslateCommandFromOptionName)
            ?.Value;

        // Parse the input text.
        var sanitizedText = FormatUtility.SanitizeText(text);
        if (string.IsNullOrWhiteSpace(sanitizedText))
        {
            _log.EmptySourceText();
            await command.Command.RespondAsync(
                "Nothing to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });
            return Unit.Value;
        }

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
                sourceLanguage);

            if (translationResult.TranslatedText == sanitizedText)
            {
                _log.FailureToDetectSourceLanguage();

                await command.Command.RespondAsync(
                    "Couldn't detect the source language to translate from or the result is the same.",
                    ephemeral: true,
                    options: new RequestOptions { CancelToken = cancellationToken });

                return Unit.Value;
            }

            await command.Command.RespondAsync(
                $@"{MentionUtils.MentionUser(command.Command.User.Id)} translated text using {translationProvider.ProviderName} from {Format.Italics(sourceLanguage?.Name ?? translationResult.DetectedLanguageName)}:
{Format.Quote(sanitizedText)}
To {Format.Italics(translationResult.TargetLanguageName)}:
{Format.Quote(translationResult.TranslatedText)}",
                options: new RequestOptions { CancelToken = cancellationToken });
        }
        catch (Exception ex)
        {
            _log.TranslationFailure(ex, translationProvider.GetType());
        }

        return Unit.Value;
    }

    /// <summary>
    /// Registers slash commands for the bot.
    /// </summary>
    /// <param name="command">The Mediator command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask<Unit> Handle(RegisterSlashCommands command, CancellationToken cancellationToken)
    {
        IReadOnlyList<IGuild> guilds = command.Guild != null
            ? new List<IGuild> { command.Guild }
            : (await _client.GetGuildsAsync(options: new RequestOptions { CancelToken = cancellationToken })).ToList();

        if (!guilds.Any())
        {
            return Unit.Value;
        }

        // Translate command.
        var translationProvider = _translationProviders[0];

        // Gather list of language choices for the command's options.
        List<SupportedLanguage> supportedLangChoices;
        if (translationProvider.TranslateCommandLangCodes == null)
        {
            // If no lang codes are specified, take the first up to the max options limit.
            supportedLangChoices = translationProvider.SupportedLanguages.Take(SlashCommandBuilder.MaxOptionsCount)
                .ToList();
        }
        else
        {
            // Get valid specified lang codes up to the limit.
            supportedLangChoices = translationProvider.SupportedLanguages.Where(
                    l => translationProvider.TranslateCommandLangCodes.Contains(l.LangCode))
                .Take(SlashCommandBuilder.MaxOptionsCount)
                .ToList();

            // If there are less languages found than the max options and more supported languages,
            // get the rest up to the max options limit.
            if (supportedLangChoices.Count < SlashCommandBuilder.MaxOptionsCount
                && translationProvider.SupportedLanguages.Count > supportedLangChoices.Count)
            {
                supportedLangChoices.AddRange(
                    translationProvider.SupportedLanguages.Where(l => !supportedLangChoices.Contains(l))
                        .Take(SlashCommandBuilder.MaxOptionsCount - supportedLangChoices.Count)
                        .ToList());
            }
        }

        // Convert the list of supported languages to command choices and sort alphabetically.
        var langChoices = supportedLangChoices.Select(
                l => new ApplicationCommandOptionChoiceProperties
                {
                    Name = l.Name.Truncate(SlashCommandBuilder.MaxNameLength),
                    Value = l.LangCode
                })
            .OrderBy(c => c.Name)
            .ToList();

        var translateFromOption = new SlashCommandOptionBuilder()
            .WithName(SlashCommandConstants.TranslateCommandFromOptionName)
            .WithDescription("The language to translate from.")
            .WithType(ApplicationCommandOptionType.String);

        translateFromOption.Choices = langChoices;

        var translateToOption = new SlashCommandOptionBuilder()
            .WithName(SlashCommandConstants.TranslateCommandToOptionName)
            .WithDescription("The language to translate to.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);

        translateToOption.Choices = langChoices;

        var translateCommand = new SlashCommandBuilder().WithName(SlashCommandConstants.TranslateCommandName)
            .WithDescription("Translate text from one language to another.")
            .AddOption(translateFromOption)
            .AddOption(translateToOption)
            .AddOption(
                new SlashCommandOptionBuilder().WithName(SlashCommandConstants.TranslateCommandTextOptionName)
                    .WithDescription("The text to be translated.")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true))
            .Build();

        foreach (var guild in guilds)
        {
            try
            {
                await guild.CreateApplicationCommandAsync(
                    translateCommand,
                    new RequestOptions { CancelToken = cancellationToken });
            }
            catch (HttpException exception)
            {
                _log.FailedToRegisterCommandForGuild(guild.Id, JsonSerializer.Serialize(exception.Errors));
            }
        }

        return Unit.Value;
    }

    /// <summary>
    /// Delegates slash command executed events to the right handler.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask Handle(SlashCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Command.Data.Name == SlashCommandConstants.TranslateCommandName)
        {
            await _mediator.Send(
                new ProcessTranslateSlashCommand { Command = notification.Command },
                cancellationToken);
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
            Level = LogLevel.Error,
            Message = "Failed to register slash command for guild ID {guildId} with error(s): {errors}")]
        public partial void FailedToRegisterCommandForGuild(ulong guildId, string errors);

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
