using System.Text.Json;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using Humanizer;

namespace DiscordTranslationBot.Notifications.Handlers;

public sealed partial class RegisterDiscordCommandsHandler
    : INotificationHandler<ReadyNotification>,
        INotificationHandler<JoinedGuildNotification>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly TranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Instantiates a new instance of the <see cref="RegisterDiscordCommandsHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviderFactory">Translation provider factory.</param>
    /// <param name="logger">Logger to use.</param>
    public RegisterDiscordCommandsHandler(
        IDiscordClient client,
        TranslationProviderFactory translationProviderFactory,
        ILogger<RegisterDiscordCommandsHandler> logger)
    {
        _client = client;
        _translationProviderFactory = translationProviderFactory;
        _log = new Log(logger);
    }

#pragma warning disable AsyncFixer01
    public async ValueTask Handle(JoinedGuildNotification notification, CancellationToken cancellationToken)
    {
        await RegisterDiscordCommandsAsync([notification.Guild], cancellationToken);
    }
#pragma warning restore AsyncFixer01

    public async ValueTask Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        var guilds = await _client.GetGuildsAsync(options: new RequestOptions { CancelToken = cancellationToken });
        if (guilds.Count == 0)
        {
            return;
        }

        await RegisterDiscordCommandsAsync(guilds, cancellationToken);
    }

    public async Task RegisterDiscordCommandsAsync(
        IReadOnlyCollection<IGuild> guilds,
        CancellationToken cancellationToken)
    {
        _log.RegisteringCommandsForGuilds(guilds.Count, guilds.Select(x => x.Id).ToArray());

        var discordCommandsToRegister = new List<ApplicationCommandProperties>();
        GetMessageCommands(discordCommandsToRegister);
        GetSlashCommands(discordCommandsToRegister);

        if (discordCommandsToRegister.Count == 0)
        {
            return;
        }

        foreach (var guild in guilds)
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
                _log.FailedToRegisterCommandsForGuild(guild.Id, JsonSerializer.Serialize(exception.Errors));
            }
        }
    }

    /// <summary>
    /// Gets message commands to register.
    /// </summary>
    /// <param name="discordCommandsToRegister">Discord commands to register.</param>
    private static void GetMessageCommands(List<ApplicationCommandProperties> discordCommandsToRegister)
    {
        discordCommandsToRegister.Add(
            new MessageCommandBuilder().WithName(MessageCommandConstants.TranslateAuto.CommandName).Build());

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
                    Name = l.Name.Truncate(SlashCommandOptionBuilder.ChoiceNameMaxLength),
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

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to register commands for guild ID {guildId} with error(s): {errors}")]
        public partial void FailedToRegisterCommandsForGuild(ulong guildId, string errors);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Registering commands for {guildCount} guild(s) {guildIds}.")]
        public partial void RegisteringCommandsForGuilds(int guildCount, ulong[] guildIds);
    }
}
