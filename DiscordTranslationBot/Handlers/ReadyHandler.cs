using System.Text.Json;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordTranslationBot.Commands.Ready;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using Humanizer;
using Mediator;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for the Ready Discord event.
/// </summary>
public sealed partial class ReadyHandler
    : INotificationHandler<ReadyNotification>,
        ICommandHandler<RegisterSlashCommands>
{
    private readonly DiscordSocketClient _client;
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IList<ITranslationProvider> _translationProviders;

    /// <summary>
    /// Instantiates a new instance of the <see cref="ReadyHandler"/> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="translationProviders">Translation providers.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="logger">Logger to use.</param>
    public ReadyHandler(
        IMediator mediator,
        IEnumerable<ITranslationProvider> translationProviders,
        DiscordSocketClient client,
        ILogger<ReadyHandler> logger
    )
    {
        _mediator = mediator;
        _translationProviders = translationProviders.ToList();
        _client = client;
        _log = new Log(logger);
    }

    /// <summary>
    /// Registers slash commands for the bot.
    /// </summary>
    /// <param name="command">The Mediator command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async ValueTask<Unit> Handle(
        RegisterSlashCommands command,
        CancellationToken cancellationToken
    )
    {
        var guilds = _client.Guilds.ToList();
        if (!guilds.Any())
        {
            throw new InvalidOperationException("No guilds found.");
        }

        // Translate command.
        var translationProvider = _translationProviders.First();

        // Gather list of language choices for the command's options.
        List<SupportedLanguage> supportedLangChoices;
        if (translationProvider.TranslateCommandLangCodes == null)
        {
            // If no lang codes are specified, take the first up to the max options limit.
            supportedLangChoices = translationProvider.SupportedLanguages
                .Take(SlashCommandBuilder.MaxOptionsCount)
                .ToList();
        }
        else
        {
            // Get valid specified lang codes up to the limit.
            supportedLangChoices = translationProvider.SupportedLanguages
                .Where(l => translationProvider.TranslateCommandLangCodes.Contains(l.LangCode))
                .Take(SlashCommandBuilder.MaxOptionsCount)
                .ToList();

            // If there are less languages found than the max options and more supported languages,
            // get the rest up to the max options limit.
            if (
                supportedLangChoices.Count < SlashCommandBuilder.MaxOptionsCount
                && translationProvider.SupportedLanguages.Count > supportedLangChoices.Count
            )
            {
                supportedLangChoices.AddRange(
                    translationProvider.SupportedLanguages
                        .Where(l => !supportedLangChoices.Contains(l))
                        .Take(SlashCommandBuilder.MaxOptionsCount - supportedLangChoices.Count)
                        .ToList()
                );
            }
        }

        // Convert the list of supported languages to command choices and sort alphabetically.
        var langChoices = supportedLangChoices
            .Select(
                l =>
                    new ApplicationCommandOptionChoiceProperties
                    {
                        Name = l.Name.Truncate(SlashCommandBuilder.MaxNameLength),
                        Value = l.LangCode
                    }
            )
            .OrderBy(c => c.Name)
            .ToList();

        var translateFromOption = new SlashCommandOptionBuilder()
            .WithName(CommandConstants.TranslateCommandFromOptionName)
            .WithDescription("The language to translate from.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);

        // Initialize from option with auto-detect option.
        translateFromOption.Choices = new List<ApplicationCommandOptionChoiceProperties>
        {
            new()
            {
                Name = CommandConstants.TranslateCommandFromOptionAutoName,
                Value = CommandConstants.TranslateCommandFromOptionAutoValue
            }
        };

        translateFromOption.Choices.AddRange(
            langChoices.Take(SlashCommandBuilder.MaxOptionsCount - 1)
        );

        var translateToOption = new SlashCommandOptionBuilder()
            .WithName(CommandConstants.TranslateCommandToOptionName)
            .WithDescription("The language to translate to.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);

        translateToOption.Choices = langChoices;

        var translateCommand = new SlashCommandBuilder()
            .WithName(CommandConstants.TranslateCommandName)
            .WithDescription("Translate text from one language to another.")
            .AddOption(translateFromOption)
            .AddOption(translateToOption)
            .AddOption(
                new SlashCommandOptionBuilder()
                    .WithName(CommandConstants.TranslateCommandTextOptionName)
                    .WithDescription("The text to be translated.")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true)
            );

        foreach (var guild in guilds)
        {
            try
            {
                await guild.CreateApplicationCommandAsync(
                    translateCommand.Build(),
                    new RequestOptions { CancelToken = cancellationToken }
                );
            }
            catch (HttpException exception)
            {
                _log.FailedToRegisterGuildCommand(
                    guild.Id,
                    JsonSerializer.Serialize(exception.Errors)
                );
            }
        }

        return Unit.Value;
    }

    /// <summary>
    /// Delegates ready events to the correct handler.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask Handle(
        ReadyNotification notification,
        CancellationToken cancellationToken
    )
    {
        await _mediator.Send(new RegisterSlashCommands(), cancellationToken);
    }

    private sealed partial class Log
    {
        private readonly ILogger<ReadyHandler> _logger;

        public Log(ILogger<ReadyHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to register guild command for guild ID {guildId} with error(s): {errors}"
        )]
        public partial void FailedToRegisterGuildCommand(ulong guildId, string errors);
    }
}
