using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using Humanizer;

namespace DiscordTranslationBot.Commands.DiscordCommands;

public sealed class RegisterDiscordCommands : ICommand
{
    /// <summary>
    /// The guilds to register Discord commands for.
    /// </summary>
    [Required]
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
    private readonly TranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Instantiates a new instance of the <see cref="RegisterDiscordCommandsHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviderFactory">Translation provider factory.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public RegisterDiscordCommandsHandler(
        IDiscordClient client,
        TranslationProviderFactory translationProviderFactory,
        IMediator mediator,
        ILogger<RegisterDiscordCommandsHandler> logger)
    {
        _client = client;
        _translationProviderFactory = translationProviderFactory;
        _mediator = mediator;
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
            try
            {
                // Use bulk overwrite method instead of create method to ensure commands are consistent with those that are added.
                await guild.BulkOverwriteApplicationCommandsAsync(
                    [..discordCommandsToRegister],
                    new RequestOptions { CancelToken = cancellationToken });
            }
            catch (HttpException exception)
            {
                LogFailedToRegisterCommandsForGuild(guild.Id, JsonSerializer.Serialize(exception.Errors));
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

        discordCommandsToRegister.Add(
            new MessageCommandBuilder().WithName(MessageCommandConstants.TranslateTo.CommandName).Build());
    }

    /// <summary>
    /// Gets slash commands to register.
    /// </summary>
    /// <param name="discordCommandsToRegister">Discord commands to register.</param>
    private void GetSlashCommands(List<ApplicationCommandProperties> discordCommandsToRegister)
    {
        // Translate command.
        // Only the first translation provider is supported as the slash command options can only be registered with one provider's supported languages.
        // Convert the list of supported languages to command choices.
        var langChoices = _translationProviderFactory
            .GetSupportedLanguagesForOptions()
            .Select(
                l => new ApplicationCommandOptionChoiceProperties
                {
                    Name = l.Name.Truncate(SlashCommandBuilder.MaxNameLength),
                    Value = l.LangCode
                })
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
        Message = "Failed to register commands for guild ID {guildId} with error(s): {errors}")]
    private partial void LogFailedToRegisterCommandsForGuild(ulong guildId, string errors);
}
