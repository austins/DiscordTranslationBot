using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using Humanizer;

namespace DiscordTranslationBot.Commands.DiscordCommands;

public sealed class RegisterDiscordCommands : ICommand
{
    /// <summary>
    /// The guilds to register Discord commands for.
    /// </summary>
    [MinLength(1)]
    public required IEnumerable<IGuild> Guilds { get; init; }
}

public sealed partial class RegisterDiscordCommandsHandler
    : ICommandHandler<RegisterDiscordCommands>,
        INotificationHandler<ReadyEvent>,
        INotificationHandler<JoinedGuildEvent>
{
    private readonly IDiscordClient _client;
    private readonly ILogger<RegisterDiscordCommandsHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;

    /// <summary>
    /// Instantiates a new instance of the <see cref="RegisterDiscordCommandsHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviders">Translation providers.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public RegisterDiscordCommandsHandler(
        IDiscordClient client,
        IEnumerable<TranslationProviderBase> translationProviders,
        IMediator mediator,
        ILogger<RegisterDiscordCommandsHandler> logger)
    {
        _client = client;
        _mediator = mediator;
        _translationProviders = translationProviders.ToList();
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(RegisterDiscordCommands command, CancellationToken cancellationToken)
    {
        var discordCommandsToRegister = new List<ApplicationCommandProperties>();
        GetMessageCommands(discordCommandsToRegister);
        GetSlashCommands(discordCommandsToRegister);

        if (discordCommandsToRegister.Count == 0)
        {
            return Unit.Value;
        }

        foreach (var guild in command.Guilds)
        {
            foreach (var discordCommand in discordCommandsToRegister)
            {
                try
                {
                    await guild.CreateApplicationCommandAsync(
                        discordCommand,
                        new RequestOptions { CancelToken = cancellationToken });
                }
                catch (HttpException exception)
                {
                    LogFailedToRegisterCommandForGuild(guild.Id, JsonSerializer.Serialize(exception.Errors));
                }
            }
        }

        return Unit.Value;
    }

#pragma warning disable AsyncFixer01
    public async ValueTask Handle(JoinedGuildEvent notification, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RegisterDiscordCommands { Guilds = [notification.Guild] }, cancellationToken);
    }
#pragma warning restore AsyncFixer01

    public async ValueTask Handle(ReadyEvent notification, CancellationToken cancellationToken)
    {
        var guilds = await _client.GetGuildsAsync(options: new RequestOptions { CancelToken = cancellationToken });
        if (guilds.Count == 0)
        {
            return;
        }

        await _mediator.Send(new RegisterDiscordCommands { Guilds = guilds }, cancellationToken);
    }

    /// <summary>
    /// Gets message commands to register.
    /// </summary>
    /// <param name="discordCommandsToRegister">Discord commands to register.</param>
    private static void GetMessageCommands(List<ApplicationCommandProperties> discordCommandsToRegister)
    {
        discordCommandsToRegister.Add(
            new MessageCommandBuilder().WithName(MessageCommandConstants.Translate.CommandName).Build());
    }

    /// <summary>
    /// Gets slash commands to register.
    /// </summary>
    /// <param name="discordCommandsToRegister">Discord commands to register.</param>
    private void GetSlashCommands(List<ApplicationCommandProperties> discordCommandsToRegister)
    {
        // Translate command.
        // Only the first translation provider is supported as the slash command options can only be registered with one provider's supported languages.
        var translationProvider = _translationProviders[0];

        // Gather list of language choices for the command's options.
        List<SupportedLanguage> supportedLangChoices;
        if (translationProvider.TranslateCommandLangCodes is null)
        {
            // If no lang codes are specified, take the first up to the max options limit.
            supportedLangChoices = translationProvider
                .SupportedLanguages
                .Take(SlashCommandBuilder.MaxOptionsCount)
                .ToList();
        }
        else
        {
            // Get valid specified lang codes up to the limit.
            supportedLangChoices = translationProvider
                .SupportedLanguages
                .Where(l => translationProvider.TranslateCommandLangCodes.Contains(l.LangCode))
                .Take(SlashCommandBuilder.MaxOptionsCount)
                .ToList();

            // If there are less languages found than the max options and more supported languages,
            // get the rest up to the max options limit.
            if (supportedLangChoices.Count < SlashCommandBuilder.MaxOptionsCount
                && translationProvider.SupportedLanguages.Count > supportedLangChoices.Count)
            {
                supportedLangChoices.AddRange(
                    translationProvider
                        .SupportedLanguages
                        .Where(l => !supportedLangChoices.Contains(l))
                        .Take(SlashCommandBuilder.MaxOptionsCount - supportedLangChoices.Count)
                        .ToList());
            }
        }

        // Convert the list of supported languages to command choices and sort alphabetically.
        var langChoices = supportedLangChoices
            .Select(
                l => new ApplicationCommandOptionChoiceProperties
                {
                    Name = l.Name?.Truncate(SlashCommandBuilder.MaxNameLength) ?? l.LangCode,
                    Value = l.LangCode
                })
            .OrderBy(c => c.Name)
            .ToList();

        var translateFromOption = new SlashCommandOptionBuilder()
            .WithName(SlashCommandConstants.Translate.CommandFromOptionName)
            .WithDescription("The language to translate from.")
            .WithType(ApplicationCommandOptionType.String);

        translateFromOption.Choices = langChoices;

        var translateToOption = new SlashCommandOptionBuilder()
            .WithName(SlashCommandConstants.Translate.CommandToOptionName)
            .WithDescription("The language to translate to.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);

        translateToOption.Choices = langChoices;

        discordCommandsToRegister.Add(
            new SlashCommandBuilder()
                .WithName(SlashCommandConstants.Translate.CommandName)
                .WithDescription("Translate text from one language to another.")
                .AddOption(translateFromOption)
                .AddOption(translateToOption)
                .AddOption(
                    new SlashCommandOptionBuilder()
                        .WithName(SlashCommandConstants.Translate.CommandTextOptionName)
                        .WithDescription("The text to be translated.")
                        .WithType(ApplicationCommandOptionType.String)
                        .WithRequired(true))
                .Build());
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to register slash command for guild ID {guildId} with error(s): {errors}")]
    private partial void LogFailedToRegisterCommandForGuild(ulong guildId, string errors);
}
