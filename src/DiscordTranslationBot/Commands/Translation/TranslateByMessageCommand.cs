﻿using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Discord.Services;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Utilities;
using Humanizer;

namespace DiscordTranslationBot.Commands.Translation;

public sealed class TranslateByMessageCommand : ICommand
{
    /// <summary>
    /// The message command.
    /// </summary>
    [Required]
    public required IMessageCommandInteraction MessageCommand { get; init; }
}

/// <summary>
/// Handler for the translate message command.
/// </summary>
public sealed partial class TranslateByMessageCommandHandler
    : ICommandHandler<TranslateByMessageCommand>,
        INotificationHandler<MessageCommandExecutedEvent>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IMessageHelper _messageHelper;
    private readonly TranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateByMessageCommandHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviderFactory">Translation provider factory to use.</param>
    /// <param name="messageHelper">Message helper to use.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateByMessageCommandHandler(
        IDiscordClient client,
        TranslationProviderFactory translationProviderFactory,
        IMessageHelper messageHelper,
        IMediator mediator,
        ILogger<TranslateByMessageCommandHandler> logger)
    {
        _client = client;
        _translationProviderFactory = translationProviderFactory;
        _messageHelper = messageHelper;
        _mediator = mediator;
        _log = new Log(logger);
    }

    /// <summary>
    /// Translates the message interacted with to the user's locale.
    /// </summary>
    /// <param name="command">The Mediator command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask<Unit> Handle(TranslateByMessageCommand command, CancellationToken cancellationToken)
    {
        if (command.MessageCommand.Data.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await command.MessageCommand.RespondAsync(
                "Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return Unit.Value;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(command.MessageCommand.Data.Message.Content);
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await command.MessageCommand.RespondAsync(
                "No text to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return Unit.Value;
        }

        await command.MessageCommand.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

        var userLocale = command.MessageCommand.UserLocale;

        string? providerName = null;
        TranslationResult? translationResult = null;
        foreach (var translationProvider in _translationProviderFactory.Providers)
        {
            try
            {
                var targetLanguage =
                    translationProvider.SupportedLanguages.FirstOrDefault(l => l.LangCode == userLocale);

                if (targetLanguage is null)
                {
                    var indexOfHyphen = userLocale.IndexOf('-', StringComparison.Ordinal);
                    if (indexOfHyphen > 0)
                    {
                        targetLanguage =
                            translationProvider.SupportedLanguages.FirstOrDefault(
                                l => l.LangCode == userLocale[..indexOfHyphen]);
                    }
                }

                if (targetLanguage is null)
                {
                    _log.UnsupportedLocale(userLocale, translationProvider.ProviderName);
                    continue;
                }

                providerName = translationProvider.ProviderName;

                translationResult = await translationProvider.TranslateAsync(
                    targetLanguage,
                    sanitizedMessage,
                    cancellationToken);

                break;
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, translationProvider.GetType());
            }
        }

        if (translationResult is null)
        {
            // Send message if no translation providers support the locale.
            await command.MessageCommand.FollowupAsync(
                $"Your locale {userLocale} isn't supported for translation via this action.",
                options: new RequestOptions { CancelToken = cancellationToken });

            return Unit.Value;
        }

        if (translationResult.TranslatedText == sanitizedMessage)
        {
            _log.FailureToDetectSourceLanguage();

            await command.MessageCommand.FollowupAsync(
                "The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
                options: new RequestOptions { CancelToken = cancellationToken });

            return Unit.Value;
        }

        var fromHeading = $"By {MentionUtils.MentionUser(command.MessageCommand.Data.Message.Author.Id)}";
        if (!string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode))
        {
            fromHeading +=
                $" from {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)}";
        }

        var toHeading =
            $"To {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName})";

        var description = $"""
                           {Format.Bold(fromHeading)}:
                           {sanitizedMessage.Truncate(50)}

                           {Format.Bold(toHeading)}:
                           {translationResult.TranslatedText}
                           """;

        await command.MessageCommand.FollowupAsync(
            embed: new EmbedBuilder()
                .WithTitle("Translated Message")
                .WithUrl(_messageHelper.GetJumpUrl(command.MessageCommand.Data.Message).AbsoluteUri)
                .WithDescription(description)
                .Build(),
            options: new RequestOptions { CancelToken = cancellationToken });

        return Unit.Value;
    }

    public async ValueTask Handle(MessageCommandExecutedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.MessageCommand.Data.Name != MessageCommandConstants.Translate.CommandName)
        {
            return;
        }

        await _mediator.Send(
            new TranslateByMessageCommand { MessageCommand = notification.MessageCommand },
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

        [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported locale {locale} for {providerName}.")]
        public partial void UnsupportedLocale(string locale, string providerName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceMessage();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerType}.")]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}
