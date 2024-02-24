using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Countries.Services;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Utilities;
using ReactionMetadata = DiscordTranslationBot.Discord.Models.ReactionMetadata;

namespace DiscordTranslationBot.Commands.Translation;

public sealed class TranslateByFlagEmojiReaction : IRequest
{
    [Required]
    public required Country Country { get; init; }

    /// <summary>
    /// The user message.
    /// </summary>
    [Required]
    public required IUserMessage Message { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    [Required]
    public required ReactionMetadata ReactionMetadata { get; init; }
}

/// <summary>
/// Handler for translating by a flag emoji reaction.
/// </summary>
public sealed partial class TranslateByFlagEmojiReactionHandler
    : IRequestHandler<TranslateByFlagEmojiReaction>, INotificationHandler<ReactionAddedEvent>
{
    private readonly IDiscordClient _client;
    private readonly ICountryService _countryService;
    private readonly Log _log;
    private readonly IMediator _mediator;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateByFlagEmojiReactionHandler" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="countryService">Country service to use.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TranslateByFlagEmojiReactionHandler(
        IDiscordClient client,
        IEnumerable<TranslationProviderBase> translationProviders,
        ICountryService countryService,
        IMediator mediator,
        ILogger<TranslateByFlagEmojiReactionHandler> logger)
    {
        _client = client;
        _translationProviders = translationProviders.ToList();
        _countryService = countryService;
        _mediator = mediator;
        _log = new Log(logger);
    }

    public async Task Handle(ReactionAddedEvent notification, CancellationToken cancellationToken)
    {
        if (!_countryService.TryGetCountryByEmoji(notification.Reaction.Emote.Name, out var country))
        {
            return;
        }

        await _mediator.Send(
            new TranslateByFlagEmojiReaction
            {
                Country = country,
                Message = await notification.Message.GetOrDownloadAsync(),
                ReactionMetadata = new ReactionMetadata
                {
                    UserId = notification.Reaction.UserId,
                    Emote = notification.Reaction.Emote
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Translates any message that got a flag emoji reaction on it.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(TranslateByFlagEmojiReaction request, CancellationToken cancellationToken)
    {
        if (request.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await request.Message.RemoveReactionAsync(
                request.ReactionMetadata.Emote,
                request.ReactionMetadata.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        var sanitizedMessage = FormatUtility.SanitizeText(request.Message.Content);
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _log.EmptySourceMessage();

            await request.Message.RemoveReactionAsync(
                request.ReactionMetadata.Emote,
                request.ReactionMetadata.UserId,
                new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        string? providerName = null;
        TranslationResult? translationResult = null;
        foreach (var translationProvider in _translationProviders)
        {
            try
            {
                providerName = translationProvider.ProviderName;

                translationResult = await translationProvider.TranslateByCountryAsync(
                    request.Country,
                    sanitizedMessage,
                    cancellationToken);

                break;
            }
            catch (LanguageNotSupportedForCountryException ex)
            {
                // Send message if this is the last available translation provider.
                if (translationProvider == _translationProviders[^1])
                {
                    _log.LanguageNotSupportedForCountry(ex, translationProvider.GetType(), request.Country.Name);

                    await _mediator.Send(
                        new SendTempReply
                        {
                            Text = ex.Message,
                            ReactionMetadata = request.ReactionMetadata,
                            SourceMessage = request.Message
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
            await request.Message.RemoveReactionAsync(
                request.ReactionMetadata.Emote,
                request.ReactionMetadata.UserId,
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
                    ReactionMetadata = request.ReactionMetadata,
                    SourceMessage = request.Message
                },
                cancellationToken);

            return;
        }

        // Send the reply message.
        var replyText = !string.IsNullOrWhiteSpace(translationResult.DetectedLanguageCode)
            ? $"""
               Translated message from {Format.Italics(translationResult.DetectedLanguageName ?? translationResult.DetectedLanguageCode)} to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):
               {Format.BlockQuote(translationResult.TranslatedText)}
               """
            : $"""
               Translated message to {Format.Italics(translationResult.TargetLanguageName ?? translationResult.TargetLanguageCode)} ({providerName}):
               {Format.BlockQuote(translationResult.TranslatedText)}
               """;

        await _mediator.Send(
            new SendTempReply
            {
                Text = replyText,
                ReactionMetadata = request.ReactionMetadata,
                SourceMessage = request.Message,
                DeletionDelay = TimeSpan.FromSeconds(20)
            },
            cancellationToken);
    }

    private sealed partial class Log
    {
        private readonly ILogger<TranslateByFlagEmojiReactionHandler> _logger;

        public Log(ILogger<TranslateByFlagEmojiReactionHandler> logger)
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
