using Discord;
using Discord.Net;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;
using DiscordTranslationBot.Utilities;
using Humanizer;
using System.Globalization;
using System.Net;

namespace DiscordTranslationBot.Notifications.Handlers;

internal sealed partial class TranslateToMessageCommandHandler
    : INotificationHandler<MessageCommandExecutedNotification>, INotificationHandler<ModalSubmittedNotification>
{
    private readonly IDiscordClient _client;
    private readonly Log _log;
    private readonly IMessageHelper _messageHelper;
    private readonly ITranslationRateLimiter _rateLimiter;
    private readonly ITranslationProviderFactory _translationProviderFactory;

    public TranslateToMessageCommandHandler(
        IDiscordClient client,
        ITranslationProviderFactory translationProviderFactory,
        IMessageHelper messageHelper,
        ITranslationRateLimiter rateLimiter,
        ILogger<TranslateToMessageCommandHandler> logger)
    {
        _client = client;
        _translationProviderFactory = translationProviderFactory;
        _messageHelper = messageHelper;
        _rateLimiter = rateLimiter;
        _log = new Log(logger);
    }

    public async ValueTask Handle(MessageCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Interaction.Data.Name != MessageCommandConstants.TranslateTo.CommandName)
        {
            return;
        }

        if (notification.Interaction.Data.Message.Author.Id == _client.CurrentUser?.Id)
        {
            _log.TranslatingBotMessageDisallowed();

            await notification.Interaction.RespondAsync(
                $"{NeoSmart.Unicode.Emoji.NoEntry} Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        // Check if message can be translated first.
        if (string.IsNullOrWhiteSpace(FormatUtility.SanitizeText(notification.Interaction.Data.Message.Content)))
        {
            _log.EmptySourceText();

            // We must acknowledge and respond to the message command.
            await notification.Interaction.RespondAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} No text to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        await notification.Interaction.RespondWithModalAsync(
            BuildModal(notification.Interaction.Data.Message.Id),
            new RequestOptions { CancelToken = cancellationToken });
    }

    public async ValueTask Handle(ModalSubmittedNotification notification, CancellationToken cancellationToken)
    {
        var modalId = notification.Interaction.Data.CustomId;
        if (!modalId.StartsWith(MessageCommandConstants.TranslateTo.ModalIdPrefix, StringComparison.Ordinal)
            || !ulong.TryParse(
                modalId.AsSpan(MessageCommandConstants.TranslateTo.ModalIdPrefix.Length),
                CultureInfo.InvariantCulture,
                out var referencedMessageId))
        {
            return;
        }

        if (!_rateLimiter.TryAcquire(notification.Interaction.User.Id))
        {
            await notification.Interaction.RespondAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} {ITranslationRateLimiter.RateLimitedMessage}",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        await notification.Interaction.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

        // Only the first translation provider is supported as the language options were built with one provider's supported languages.
        var translationProvider = _translationProviderFactory.PrimaryProvider;

        try
        {
            var channel = notification.Interaction.ChannelId is { } channelId
                ? await _client.GetChannelAsync(
                    channelId,
                    options: new RequestOptions { CancelToken = cancellationToken }) as IMessageChannel
                : null;

            var referencedMessage = channel is null
                ? null
                : await channel.GetMessageAsync(
                    referencedMessageId,
                    options: new RequestOptions { CancelToken = cancellationToken });

            if (referencedMessage is null)
            {
                _log.ReferencedMessageNotFound(referencedMessageId);

                await notification.Interaction.FollowupAsync(
                    $"{NeoSmart.Unicode.Emoji.Warning} The original message was deleted.",
                    ephemeral: true,
                    options: new RequestOptions { CancelToken = cancellationToken });

                return;
            }

            // The message could have been edited since the modal was opened.
            var sanitizedText = FormatUtility.SanitizeText(referencedMessage.Content);
            if (string.IsNullOrWhiteSpace(sanitizedText))
            {
                _log.EmptySourceText();

                await notification.Interaction.FollowupAsync(
                    $"{NeoSmart.Unicode.Emoji.Warning} No text to translate.",
                    ephemeral: true,
                    options: new RequestOptions { CancelToken = cancellationToken });

                return;
            }

            var modalComponents = notification.Interaction.Data.Components;

            var selectedLangCode = modalComponents
                .First(x => x.CustomId == MessageCommandConstants.TranslateTo.LanguageSelectMenuId)
                .Values
                .First();

            var share = modalComponents
                            .FirstOrDefault(x => x.CustomId == MessageCommandConstants.TranslateTo.ShareCheckboxId)
                            ?.BoolValue
                        == true;

            var targetLanguage = new SupportedLanguage(
                selectedLangCode,
                translationProvider.SupportedLanguages[selectedLangCode]);

            var translationResult = await translationProvider.TranslateAsync(
                targetLanguage,
                sanitizedText,
                cancellationToken);

            if (translationResult.TranslatedText == sanitizedText)
            {
                _log.FailureToDetectSourceLanguage();

                await notification.Interaction.FollowupAsync(
                    $"{NeoSmart.Unicode.Emoji.Warning} Couldn't detect the source language to translate from or the result is the same.",
                    ephemeral: true,
                    options: new RequestOptions { CancelToken = cancellationToken });

                return;
            }

            // Ephemeral messages cannot have a message reference, but messages sent directly to a channel can, but
            // we have a jump URL in the content text in both cases for consistency.
            if (share)
            {
                await Task.WhenAll(
                    notification.Interaction.DeleteOriginalResponseAsync(
                        new RequestOptions { CancelToken = cancellationToken }),
                    referencedMessage.Channel.SendMessageAsync(
                        _messageHelper.BuildTranslationReplyWithReference(
                            referencedMessage,
                            translationResult,
                            notification.Interaction.User.Id),
                        allowedMentions: AllowedMentions.None,
                        messageReference: new MessageReference(referencedMessageId),
                        options: new RequestOptions { CancelToken = cancellationToken }));
            }
            else
            {
                await notification.Interaction.FollowupAsync(
                    _messageHelper.BuildTranslationReplyWithReference(referencedMessage, translationResult),
                    ephemeral: true,
                    options: new RequestOptions { CancelToken = cancellationToken });
            }

            _log.TranslationSuccess(translationProvider.GetType().Name);
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
        {
            _log.MissingPermissions(ex, notification.Interaction.ChannelId);

            await notification.Interaction.FollowupAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} The bot is missing permissions in this channel.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _log.TranslationFailure(ex, translationProvider.GetType().Name);

            await notification.Interaction.FollowupAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} Failed to translate text. Please try again.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });
        }
    }

    private Modal BuildModal(ulong messageId)
    {
        var langOptions = _translationProviderFactory
            .GetSupportedLanguagesForOptions()
            .Select(l =>
                new SelectMenuOptionBuilder()
                    .WithLabel(l.Name.Truncate(SelectMenuOptionBuilder.MaxSelectLabelLength))
                    .WithValue(l.LangCode))
            .ToList();

        return new ModalBuilder()
            .WithTitle(MessageCommandConstants.TranslateTo.CommandName)
            .WithCustomId($"{MessageCommandConstants.TranslateTo.ModalIdPrefix}{messageId}")
            .AddSelectMenu(
                "Language",
                MessageCommandConstants.TranslateTo.LanguageSelectMenuId,
                langOptions,
                "Select the language to translate to...")
            .AddCheckBox(
                "Share in channel",
                MessageCommandConstants.TranslateTo.ShareCheckboxId,
                false,
                "Post the translation as a reply that everyone can see.")
            .Build();
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Translating this bot's messages isn't allowed.")]
        public partial void TranslatingBotMessageDisallowed();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceText();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Referenced message ID {messageId} was not found. It was likely deleted.")]
        public partial void ReferencedMessageNotFound(ulong messageId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Missing permissions in channel ID {channelId}.")]
        public partial void MissingPermissions(Exception ex, ulong? channelId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully translated text with {providerName}.")]
        public partial void TranslationSuccess(string providerName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerName}.")]
        public partial void TranslationFailure(Exception ex, string providerName);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}
