using System.Text.Json;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Commands.MessageCommandExecuted;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Utilities;
using Humanizer;
using Mediator;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handles the MessageCommandExecuted event of the Discord client.
/// </summary>
public sealed partial class MessageCommandExecutedHandler
    : INotificationHandler<MessageCommandExecutedNotification>,
        ICommandHandler<RegisterMessageCommands>,
        ICommandHandler<ProcessTranslateMessageCommand>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<ITranslationProvider> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageCommandExecutedHandler"/> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="logger">Logger to use.</param>
    public MessageCommandExecutedHandler(
        IMediator mediator,
        IEnumerable<ITranslationProvider> translationProviders,
        IDiscordClient client,
        ILogger<MessageCommandExecutedHandler> logger
    )
    {
        _mediator = mediator;
        _translationProviders = translationProviders.ToList();
        _client = client;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates the message interacted with to the user's locale.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask<Unit> Handle(
        ProcessTranslateMessageCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.Command.Data.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await command.Command.RespondAsync(
                "Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken }
            );

            return Unit.Value;
        }

        var userLocale = command.Command.UserLocale;

        foreach (var translationProvider in _translationProviders)
        {
            try
            {
                var targetLanguage = translationProvider.SupportedLanguages.FirstOrDefault(
                    l => l.LangCode == userLocale
                );

                if (targetLanguage == null)
                {
                    var indexOfHyphen = userLocale.IndexOf('-', StringComparison.Ordinal);
                    if (indexOfHyphen > 0)
                    {
                        targetLanguage = translationProvider.SupportedLanguages.FirstOrDefault(
                            l => l.LangCode == userLocale[..indexOfHyphen]
                        );
                    }
                }

                if (targetLanguage == null)
                {
                    _log.UnsupportedLocale(userLocale, translationProvider.ProviderName);

                    // Send message if this is the last available translation provider.
                    if (translationProvider == _translationProviders[^1])
                    {
                        await command.Command.RespondAsync(
                            $"Your locale {userLocale} isn't supported for translation via this action.",
                            ephemeral: true,
                            options: new RequestOptions { CancelToken = cancellationToken }
                        );

                        return Unit.Value;
                    }

                    continue;
                }

                var sanitizedMessage = FormatUtility.SanitizeText(
                    command.Command.Data.Message.Content
                );

                if (string.IsNullOrWhiteSpace(sanitizedMessage))
                {
                    _log.EmptySourceMessage();
                    return Unit.Value;
                }

                var translationResult = await translationProvider.TranslateAsync(
                    targetLanguage,
                    sanitizedMessage,
                    cancellationToken
                );

                if (translationResult.TranslatedText == sanitizedMessage)
                {
                    _log.FailureToDetectSourceLanguage();

                    await command.Command.RespondAsync(
                        "The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
                        ephemeral: true,
                        options: new RequestOptions { CancelToken = cancellationToken }
                    );

                    return Unit.Value;
                }

                var fromHeading =
                    $"From {MentionUtils.MentionUser(command.Command.Data.Message.Author.Id)}";

                if (!string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode))
                {
                    fromHeading +=
                        $" in {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)}";
                }

                var toHeading =
                    $"To {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({translationProvider.ProviderName})";

                var description =
                    $@"{Format.Bold(fromHeading)}:
{sanitizedMessage.Truncate(50)}

{Format.Bold(toHeading)}:
{translationResult.TranslatedText}";

                await command.Command.RespondAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Translated Message")
                        .WithUrl(command.Command.Data.Message.GetJumpUrl())
                        .WithDescription(description)
                        .Build(),
                    ephemeral: true,
                    options: new RequestOptions { CancelToken = cancellationToken }
                );
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, translationProvider.GetType());
            }
        }

        return Unit.Value;
    }

    /// <summary>
    /// Registers the message commands for the guild(s).
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask<Unit> Handle(
        RegisterMessageCommands command,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<IGuild> guilds =
            command.Guild != null
                ? new List<IGuild> { command.Guild }
                : (
                    await _client.GetGuildsAsync(
                        options: new RequestOptions { CancelToken = cancellationToken }
                    )
                ).ToList();

        if (!guilds.Any())
        {
            return Unit.Value;
        }

        var translateCommand = new MessageCommandBuilder()
            .WithName(MessageCommandConstants.TranslateCommandName)
            .Build();

        foreach (var guild in guilds)
        {
            try
            {
                await guild.CreateApplicationCommandAsync(
                    translateCommand,
                    new RequestOptions { CancelToken = cancellationToken }
                );
            }
            catch (HttpException exception)
            {
                _log.FailedToRegisterCommandForGuild(
                    guild.Id,
                    JsonSerializer.Serialize(exception.Errors)
                );
            }
        }

        return Unit.Value;
    }

    /// <summary>
    /// Delegates the MessageCommandExecuted event to the appropriate handler.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask Handle(
        MessageCommandExecutedNotification notification,
        CancellationToken cancellationToken
    )
    {
        if (notification.Command.Data.Name == MessageCommandConstants.TranslateCommandName)
        {
            await _mediator.Send(
                new ProcessTranslateMessageCommand { Command = notification.Command },
                cancellationToken
            );
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<MessageCommandExecutedHandler> _logger;

        public Log(ILogger<MessageCommandExecutedHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to register message command for guild ID {guildId} with error(s): {errors}"
        )]
        public partial void FailedToRegisterCommandForGuild(ulong guildId, string errors);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Translating this bot's messages isn't allowed."
        )]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Unsupported locale {locale} for {providerName}."
        )]
        public partial void UnsupportedLocale(string locale, string providerName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty."
        )]
        public partial void EmptySourceMessage();

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
