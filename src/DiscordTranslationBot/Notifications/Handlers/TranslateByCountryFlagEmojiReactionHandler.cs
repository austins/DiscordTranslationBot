﻿using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Utilities;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;
using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Notifications.Handlers;

/// <summary>
/// Handler for translating by a flag emoji reaction.
/// </summary>
public sealed partial class TranslateByCountryFlagEmojiReactionHandler : INotificationHandler<ReactionAddedNotification>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IMessageHelper _messageHelper;
    private readonly TranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateByCountryFlagEmojiReactionHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviderFactory">Translation provider factory to use.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="messageHelper">Message helper to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateByCountryFlagEmojiReactionHandler(
        IDiscordClient client,
        TranslationProviderFactory translationProviderFactory,
        IMediator mediator,
        IMessageHelper messageHelper,
        ILogger<TranslateByCountryFlagEmojiReactionHandler> logger)
    {
        _client = client;
        _translationProviderFactory = translationProviderFactory;
        _mediator = mediator;
        _messageHelper = messageHelper;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates any message that got a country flag emoji reaction on it.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        if (!CountryUtility.TryGetCountryByEmoji(notification.ReactionInfo.Emote.Name, out var country))
        {
            return;
        }

        if (notification.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await notification.Message.RemoveReactionAsync(
                notification.ReactionInfo.Emote,
                notification.ReactionInfo.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(notification.Message.Content);
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await notification.Message.RemoveReactionAsync(
                notification.ReactionInfo.Emote,
                notification.ReactionInfo.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        TranslationResult? translationResult = null;
        foreach (var translationProvider in _translationProviderFactory.Providers)
        {
            try
            {
                translationResult = await translationProvider.TranslateByCountryAsync(
                    country,
                    sanitizedMessage,
                    cancellationToken);

                break;
            }
            catch (LanguageNotSupportedForCountryException ex)
            {
                // Send message if this is the last available translation provider.
                if (ReferenceEquals(translationProvider, _translationProviderFactory.LastProvider))
                {
                    _log.LanguageNotSupportedForCountry(ex, translationProvider.GetType(), country.Name);

                    await _mediator.Send(
                        new SendTempReply
                        {
                            Text = ex.Message,
                            ReactionInfo = notification.ReactionInfo,
                            SourceMessage = notification.Message
                        },
                        cancellationToken);

                    return;
                }
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, translationProvider.GetType());
            }
        }

        if (translationResult is null)
        {
            await notification.Message.RemoveReactionAsync(
                notification.ReactionInfo.Emote,
                notification.ReactionInfo.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        if (translationResult.TranslatedText == sanitizedMessage)
        {
            _log.FailureToDetectSourceLanguage();

            await _mediator.Send(
                new SendTempReply
                {
                    Text = "Couldn't detect the source language to translate from or the result is the same.",
                    ReactionInfo = notification.ReactionInfo,
                    SourceMessage = notification.Message
                },
                cancellationToken);

            return;
        }

        // Send the reply message. Note: we can't send an ephemeral message as those are only for command interactions.
        await _mediator.Send(
            new SendTempReply
            {
                Text = _messageHelper.BuildTranslationReplyWithReference(
                    notification.Message,
                    translationResult,
                    notification.ReactionInfo.UserId),
                ReactionInfo = notification.ReactionInfo,
                SourceMessage = notification.Message,
                DeletionDelay = TimeSpan.FromSeconds(20)
            },
            cancellationToken);
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Translating this bot's messages isn't allowed.")]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceMessage();

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Target language code not supported. Provider {providerType} doesn't support the language code or the country {countryName} has no mapping for the language code.")]
        public partial void LanguageNotSupportedForCountry(Exception ex, Type providerType, string countryName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerType}.")]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}