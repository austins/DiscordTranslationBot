using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Services;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using Humanizer;

namespace DiscordTranslationBot.Notifications.Handlers;

public sealed class TranslateToByMessageCommandHandler
    : INotificationHandler<MessageCommandExecutedNotification>,
        INotificationHandler<SelectMenuExecutedNotification>
{
    private readonly IMessageHelper _messageHelper;
    private readonly TranslationProviderFactory _translationProviderFactory;

    public TranslateToByMessageCommandHandler(
        TranslationProviderFactory translationProviderFactory,
        IMessageHelper messageHelper)
    {
        _translationProviderFactory = translationProviderFactory;
        _messageHelper = messageHelper;
    }

    public async ValueTask Handle(MessageCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.MessageCommand.Data.Name != MessageCommandConstants.TranslateTo.CommandName)
        {
            return;
        }

        await notification.MessageCommand.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

        // Convert the list of supported languages to select menu options.
        var langOptions = _translationProviderFactory
            .GetSupportedLanguagesForOptions()
            .Select(
                l => new SelectMenuOptionBuilder()
                    .WithLabel(l.Name.Truncate(SelectMenuOptionBuilder.MaxSelectLabelLength))
                    .WithValue(l.LangCode))
            .ToList();

        var components = new ComponentBuilder()
            .WithSelectMenu(
                MessageCommandConstants.TranslateTo.SelectMenuId,
                langOptions,
                "Select the language to translate to...")
            .WithButton("Translate", "buttonid1")
            .WithButton("Translate & Share", "buttonid2", ButtonStyle.Secondary)
            .Build();

        await notification.MessageCommand.FollowupAsync(
            $"What would you like to translate {_messageHelper.GetJumpUrl(notification.MessageCommand.Data.Message)} to?",
            components: components,
            ephemeral: true,
            options: new RequestOptions { CancelToken = cancellationToken });
    }

    public async ValueTask Handle(SelectMenuExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.MessageComponent.Data.CustomId != MessageCommandConstants.TranslateTo.SelectMenuId)
        {
            return;
        }

        await notification.MessageComponent.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

        // Get ID of message to translate.
        var messageId = _messageHelper.GetJumpUrlsInMessage(notification.MessageComponent.Message)[0].MessageId;
        var message = await notification.MessageComponent.Message.Channel.GetMessageAsync(messageId);

        await notification.MessageComponent.ModifyOriginalResponseAsync(
            m =>
            {
                m.Content = null;
                m.Components = null;

                m.Embed = new EmbedBuilder()
                    .WithTitle("Translated Message")
                    .WithUrl(_messageHelper.GetJumpUrl(message).AbsoluteUri)
                    .WithDescription("translated text here")
                    .Build();
            },
            new RequestOptions { CancelToken = cancellationToken });
    }
}
