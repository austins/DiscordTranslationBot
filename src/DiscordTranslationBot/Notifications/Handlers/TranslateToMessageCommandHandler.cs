using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using DiscordTranslationBot.Utilities;
using Humanizer;

namespace DiscordTranslationBot.Notifications.Handlers;

public sealed partial class TranslateToMessageCommandHandler
    : INotificationHandler<MessageCommandExecutedNotification>,
        INotificationHandler<SelectMenuExecutedNotification>,
        INotificationHandler<ButtonExecutedNotification>
{
    private readonly Log _log;
    private readonly IMessageHelper _messageHelper;
    private readonly TranslationProviderFactory _translationProviderFactory;

    public TranslateToMessageCommandHandler(
        TranslationProviderFactory translationProviderFactory,
        IMessageHelper messageHelper,
        ILogger<TranslateToMessageCommandHandler> logger)
    {
        _translationProviderFactory = translationProviderFactory;
        _messageHelper = messageHelper;
        _log = new Log(logger);
    }

    public async ValueTask Handle(ButtonExecutedNotification notification, CancellationToken cancellationToken)
    {
        var buttonId = notification.Interaction.Data.CustomId;
        if (buttonId != MessageCommandConstants.TranslateTo.TranslateButtonId
            && buttonId != MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)
        {
            return;
        }

        await notification.Interaction.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

        var referencedMessageId = notification.Interaction.Message.Reference.MessageId.GetValueOrDefault(
            _messageHelper.GetJumpUrlsInMessage(notification.Interaction.Message)[0].MessageId);

        var referencedMessage = await notification.Interaction.Message.Channel.GetMessageAsync(
            referencedMessageId,
            options: new RequestOptions { CancelToken = cancellationToken });

        // Parse the input text.
        var sanitizedText = FormatUtility.SanitizeText(referencedMessage.Content);
        if (string.IsNullOrWhiteSpace(sanitizedText))
        {
            _log.EmptySourceText();

            await notification.Interaction.ModifyOriginalResponseAsync(
                m =>
                {
                    m.Content = "No text to translate.";
                    m.Components = null;
                },
                new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        var selectedLanguage = GetSelectedLanguage(notification.Interaction.Message);

        // Only the first translation provider is supported as the slash command options were registered with one provider's supported languages.
        var translationProvider = _translationProviderFactory.PrimaryProvider;

        try
        {
            var targetLanguage = translationProvider.SupportedLanguages.First(l => l.LangCode == selectedLanguage);

            var translationResult = await translationProvider.TranslateAsync(
                targetLanguage,
                sanitizedText,
                cancellationToken);

            if (translationResult.TranslatedText == sanitizedText)
            {
                _log.FailureToDetectSourceLanguage();

                await notification.Interaction.ModifyOriginalResponseAsync(
                    m =>
                    {
                        m.Content = "Couldn't detect the source language to translate from or the result is the same.";
                        m.Components = null;
                    },
                    new RequestOptions { CancelToken = cancellationToken });

                return;
            }

            if (buttonId == MessageCommandConstants.TranslateTo.TranslateButtonId)
            {
                await notification.Interaction.ModifyOriginalResponseAsync(
                    m =>
                    {
                        m.Content = _messageHelper.BuildTranslationReplyWithReference(
                            referencedMessage,
                            translationResult);

                        m.Components = null;
                    },
                    new RequestOptions { CancelToken = cancellationToken });
            }
            else if (buttonId == MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)
            {
                await Task.WhenAll(
                    notification.Interaction.DeleteOriginalResponseAsync(
                        new RequestOptions { CancelToken = cancellationToken }),
                    notification.Interaction.Message.Channel.SendMessageAsync(
                        _messageHelper.BuildTranslationReplyWithReference(
                            referencedMessage,
                            translationResult,
                            notification.Interaction.User.Id),
                        messageReference: notification.Interaction.Message.Reference,
                        options: new RequestOptions { CancelToken = cancellationToken }));
            }
        }
        catch (Exception ex)
        {
            _log.TranslationFailure(ex, translationProvider.GetType());
        }
    }

    public async ValueTask Handle(MessageCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.MessageCommand.Data.Name != MessageCommandConstants.TranslateTo.CommandName)
        {
            return;
        }

        // Check if message can be translated first.
        if (string.IsNullOrWhiteSpace(FormatUtility.SanitizeText(notification.MessageCommand.Data.Message.Content)))
        {
            _log.EmptySourceText();

            // We must acknowledge and respond to the message command.
            await notification.MessageCommand.RespondAsync(
                "No text to translate.",
                ephemeral: true,
                options: new RequestOptions { CancelToken = cancellationToken });

            return;
        }

        await notification.MessageCommand.RespondAsync(
            $"What would you like to translate {_messageHelper.GetJumpUrl(notification.MessageCommand.Data.Message)} to?",
            components: BuildMessageComponents(false),
            ephemeral: true,
            options: new RequestOptions { CancelToken = cancellationToken });
    }

    public async ValueTask Handle(SelectMenuExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Interaction.Data.CustomId != MessageCommandConstants.TranslateTo.SelectMenuId)
        {
            return;
        }

        await notification.Interaction.UpdateAsync(
            m => { m.Components = BuildMessageComponents(true, notification.Interaction.Data.Values.First()); },
            new RequestOptions { CancelToken = cancellationToken });
    }

    private static string GetSelectedLanguage(IUserMessage message)
    {
        if ((message.Components.FirstOrDefault() as ActionRowComponent)?.Components.FirstOrDefault(
                x => x.CustomId == MessageCommandConstants.TranslateTo.SelectMenuId) is SelectMenuComponent
            selectMenuComponent)
        {
            return selectMenuComponent.Options.First(x => x.IsDefault == true).Value;
        }

        throw new InvalidOperationException("Failed to find select menu component in message.");
    }

    private MessageComponent BuildMessageComponents(bool buttonsEnabled, string? valueSelected = null)
    {
        // Convert the list of supported languages to select menu options.
        var langOptions = _translationProviderFactory
            .GetSupportedLanguagesForOptions()
            .Select(
                l => new SelectMenuOptionBuilder()
                    .WithLabel(l.Name.Truncate(SelectMenuOptionBuilder.MaxSelectLabelLength))
                    .WithValue(l.LangCode)
                    .WithDefault(valueSelected == l.LangCode))
            .ToList();

        return new ComponentBuilder()
            .WithSelectMenu(
                MessageCommandConstants.TranslateTo.SelectMenuId,
                langOptions,
                "Select the language to translate to...")
            .WithButton("Translate", MessageCommandConstants.TranslateTo.TranslateButtonId, disabled: !buttonsEnabled)
            .WithButton(
                "Translate & Share",
                MessageCommandConstants.TranslateTo.TranslateAndShareButtonId,
                ButtonStyle.Secondary,
                disabled: !buttonsEnabled)
            .Build();
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Nothing to translate. The sanitized source message is empty.")]
        public partial void EmptySourceText();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerType}.")]
        public partial void TranslationFailure(Exception ex, Type providerType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "Couldn't detect the source language to translate from. This could happen when the provider's detected language confidence is 0 or the source language is the same as the target language.")]
        public partial void FailureToDetectSourceLanguage();
    }
}
