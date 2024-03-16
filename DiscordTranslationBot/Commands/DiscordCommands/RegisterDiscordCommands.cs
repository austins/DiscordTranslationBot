using System.Text.Json;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using FluentValidation;
using Humanizer;
using IRequest = MediatR.IRequest;

namespace DiscordTranslationBot.Commands.DiscordCommands;

public sealed class RegisterDiscordCommands : IRequest
{
    /// <summary>
    /// The guilds to register Discord commands for.
    /// </summary>
    public required IEnumerable<IGuild> Guilds { get; init; }
}

public sealed class RegisterDiscordCommandsValidator : AbstractValidator<RegisterDiscordCommands>
{
    public RegisterDiscordCommandsValidator()
    {
        RuleFor(x => x.Guilds).NotEmpty();
    }
}

public sealed partial class RegisterDiscordCommandsHandler
    : IRequestHandler<RegisterDiscordCommands>, INotificationHandler<ReadyEvent>, INotificationHandler<JoinedGuildEvent>
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

    public Task Handle(JoinedGuildEvent notification, CancellationToken cancellationToken)
    {
        return _mediator.Send(new RegisterDiscordCommands { Guilds = [notification.Guild] }, cancellationToken);
    }

    public async Task Handle(ReadyEvent notification, CancellationToken cancellationToken)
    {
        var guilds = await _client.GetGuildsAsync(options: new RequestOptions { CancelToken = cancellationToken });
        if (guilds.Count == 0)
        {
            return;
        }

        await _mediator.Send(new RegisterDiscordCommands { Guilds = guilds }, cancellationToken);
    }

    public async Task Handle(RegisterDiscordCommands request, CancellationToken cancellationToken)
    {
        var commandsToRegister = new List<ApplicationCommandProperties>();
        GetMessageCommands(commandsToRegister);
        GetSlashCommands(commandsToRegister);

        if (commandsToRegister.Count == 0)
        {
            return;
        }

        foreach (var guild in request.Guilds)
        {
            foreach (var command in commandsToRegister)
            {
                try
                {
                    await guild.CreateApplicationCommandAsync(
                        command,
                        new RequestOptions { CancelToken = cancellationToken });
                }
                catch (HttpException exception)
                {
                    LogFailedToRegisterCommandForGuild(guild.Id, JsonSerializer.Serialize(exception.Errors));
                }
            }
        }
    }

    /// <summary>
    /// Gets message commands to register.
    /// </summary>
    /// <param name="commandsToRegister">Commands to register.</param>
    private static void GetMessageCommands(List<ApplicationCommandProperties> commandsToRegister)
    {
        commandsToRegister.Add(
            new MessageCommandBuilder().WithName(MessageCommandConstants.TranslateCommandName).Build());
    }

    /// <summary>
    /// Gets slash commands to register.
    /// </summary>
    /// <param name="commandsToRegister">Commands to register.</param>
    private void GetSlashCommands(List<ApplicationCommandProperties> commandsToRegister)
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

        commandsToRegister.Add(
            new SlashCommandBuilder()
                .WithName(SlashCommandConstants.TranslateCommandName)
                .WithDescription("Translate text from one language to another.")
                .AddOption(translateFromOption)
                .AddOption(translateToOption)
                .AddOption(
                    new SlashCommandOptionBuilder()
                        .WithName(SlashCommandConstants.TranslateCommandTextOptionName)
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
