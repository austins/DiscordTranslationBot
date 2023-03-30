using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Commands.ReactionAdded;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using DiscordTranslationBot.Utilities;
using Mediator;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handles the ReactionAdded event of the Discord client.
/// </summary>
public sealed partial class ReactionAddedHandler
    : INotificationHandler<ReactionAddedNotification>,
        ICommandHandler<ProcessFlagEmojiReaction>
{
    private readonly DiscordSocketClient _client;
    private readonly ICountryService _countryService;
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<ITranslationProvider> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionAddedHandler"/> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="countryService">Country service to use.</param>
    /// <param name="logger">Logger to use.</param>
    public ReactionAddedHandler(
        IMediator mediator,
        IEnumerable<ITranslationProvider> translationProviders,
        DiscordSocketClient client,
        ICountryService countryService,
        ILogger<ReactionAddedHandler> logger
    )
    {
        _mediator = mediator;
        _translationProviders = translationProviders.ToList();
        _client = client;
        _countryService = countryService;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates any message that got a flag emoji reaction on it.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask<Unit> Handle(
        ProcessFlagEmojiReaction command,
        CancellationToken cancellationToken
    )
    {
        if (command.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await command.Message.RemoveReactionAsync(
                command.Reaction.Emote,
                command.Reaction.UserId,
                new RequestOptions { CancelToken = cancellationToken }
            );

            return Unit.Value;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(command.Message.Content);

        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await command.Message.RemoveReactionAsync(
                command.Reaction.Emote,
                command.Reaction.UserId,
                new RequestOptions { CancelToken = cancellationToken }
            );

            return Unit.Value;
        }

        string? providerName = null;
        TranslationResult? translationResult = null;
        foreach (var translationProvider in _translationProviders)
        {
            try
            {
                providerName = translationProvider.ProviderName;

                translationResult = await translationProvider.TranslateByCountryAsync(
                    command.Country,
                    sanitizedMessage,
                    cancellationToken
                );

                break;
            }
            catch (UnsupportedCountryException ex)
            {
                _log.UnsupportedCountry(ex, command.Country.Name, translationProvider.GetType());

                // Send message if this is the last available translation provider.
                if (translationProvider == _translationProviders[^1])
                {
                    SendTempMessage(
                        ex.Message,
                        command.Reaction,
                        command.Message.Channel,
                        command.Message.Id,
                        cancellationToken
                    );

                    return Unit.Value;
                }
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, translationProvider.GetType());
            }
        }

        if (translationResult == null)
        {
            await command.Message.RemoveReactionAsync(
                command.Reaction.Emote,
                command.Reaction.UserId,
                new RequestOptions { CancelToken = cancellationToken }
            );

            return Unit.Value;
        }

        if (translationResult.TranslatedText == sanitizedMessage)
        {
            _log.FailureToDetectSourceLanguage();

            SendTempMessage(
                "Couldn't detect the source language to translate from or the result is the same.",
                command.Reaction,
                command.Message.Channel,
                command.Message.Id,
                cancellationToken
            );

            return Unit.Value;
        }

        // Send the reply message.
        var replyText = !string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode)
            ? $@"Translated message from {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)} to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):
{Format.BlockQuote(translationResult.TranslatedText)}"
            : $@"Translated message to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):
{Format.BlockQuote(translationResult.TranslatedText)}";

        SendTempMessage(
            replyText,
            command.Reaction,
            command.Message.Channel,
            command.Message.Id,
            cancellationToken,
            20
        );

        return Unit.Value;
    }

    /// <summary>
    /// Delegates reaction added events to the correct handler.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask Handle(
        ReactionAddedNotification notification,
        CancellationToken cancellationToken
    )
    {
        if (
            Emoji.IsEmoji(notification.Reaction.Emote.Name)
            && _countryService.TryGetCountry(notification.Reaction.Emote.Name, out var country)
        )
        {
            await _mediator.Send(
                new ProcessFlagEmojiReaction
                {
                    Message = notification.Message,
                    Reaction = notification.Reaction,
                    Country = country!
                },
                cancellationToken
            );
        }
    }

    /// <summary>
    /// Sends a message and then clears the reaction and message after a certain time.
    /// </summary>
    /// <param name="text">Text to send in message.</param>
    /// <param name="reaction">The reaction.</param>
    /// <param name="channel">The channel to post the message in.</param>
    /// <param name="referencedMessageId">The source message ID to reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="seconds">How many seconds the message should be shown.</param>
    private static void SendTempMessage(
        string text,
        Reaction reaction,
        IMessageChannel channel,
        ulong referencedMessageId,
        CancellationToken cancellationToken,
        uint seconds = 10
    )
    {
        using var typingState = channel.EnterTypingState();

        // Wrapped in Task.Run to not block the handler as the cleanup has a delay of over 3 seconds.
        _ = Task.Run(
            async () =>
            {
                // Send reply message.
                var replyMessage = await channel.SendMessageAsync(
                    text,
                    messageReference: new MessageReference(referencedMessageId),
                    options: new RequestOptions { CancelToken = cancellationToken }
                );

                // Cleanup.
                await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);

                // If the source message still exists, remove the reaction from it.
                var sourceMessage = await replyMessage.Channel.GetMessageAsync(
                    referencedMessageId,
                    options: new RequestOptions { CancelToken = cancellationToken }
                );

                if (sourceMessage != null)
                {
                    await sourceMessage.RemoveReactionAsync(
                        reaction.Emote,
                        reaction.UserId,
                        new RequestOptions { CancelToken = cancellationToken }
                    );
                }

                // Delete the reply message.
                await replyMessage.DeleteAsync(
                    new RequestOptions { CancelToken = cancellationToken }
                );
            },
            cancellationToken
        );
    }

    private sealed partial class Log
    {
        private readonly ILogger<ReactionAddedHandler> _logger;

        public Log(ILogger<ReactionAddedHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Translating this bot's messages isn't allowed."
        )]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty."
        )]
        public partial void EmptySourceMessage();

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Unsupported country {countryName} for {providerType}."
        )]
        public partial void UnsupportedCountry(
            Exception ex,
            string? countryName,
            Type providerType
        );

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
