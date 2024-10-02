using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Services;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Providers.Translation;
using Humanizer;

namespace DiscordTranslationBot.Notifications.Handlers;

public sealed class TranslateToByMessageCommandHandler
    : INotificationHandler<MessageCommandExecutedNotification>,
        INotificationHandler<SelectMenuExecutedNotification>,
        INotificationHandler<ButtonExecutedNotification>
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

    public async ValueTask Handle(ButtonExecutedNotification notification, CancellationToken cancellationToken)
    {
        var buttonId = notification.Interaction.Data.CustomId;
        if (buttonId != MessageCommandConstants.TranslateTo.TranslateButtonId
            && buttonId != MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)
        {
            return;
        }

        await notification.Interaction.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

        var selectedLanguage = GetSelectedLanguage(notification.Interaction.Message);
        if (selectedLanguage is null)
        {
            return;
        }

        if (buttonId == MessageCommandConstants.TranslateTo.TranslateButtonId)
        {
            await notification.Interaction.ModifyOriginalResponseAsync(
                m =>
                {
                    m.Content = null;
                    m.Components = null;

                    m.Embed = new EmbedBuilder()
                        .WithTitle("Translated Message")
                        .WithUrl(_messageHelper.GetJumpUrl(notification.Interaction.Message).AbsoluteUri)
                        .WithDescription("translated text here")
                        .Build();
                },
                new RequestOptions { CancelToken = cancellationToken });
        }
    }

    public async ValueTask Handle(MessageCommandExecutedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.MessageCommand.Data.Name != MessageCommandConstants.TranslateTo.CommandName)
        {
            return;
        }

        await notification.MessageCommand.RespondAsync(
            $"What would you like to translate {_messageHelper.GetJumpUrl(notification.MessageCommand.Data.Message)} to?",
            components: BuildMessageComponents(),
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
            m => { m.Components = BuildMessageComponents(notification.Interaction.Data.Values.First()); },
            new RequestOptions { CancelToken = cancellationToken });
    }

    private static string? GetSelectedLanguage(IUserMessage message)
    {
        if ((message.Components.FirstOrDefault() as ActionRowComponent)?.Components.FirstOrDefault(
                x => x.CustomId == MessageCommandConstants.TranslateTo.SelectMenuId) is SelectMenuComponent
            selectMenuComponent)
        {
            return selectMenuComponent.Options.FirstOrDefault(x => x.IsDefault == true)?.Value;
        }

        throw new InvalidOperationException("Failed to find select menu component in message.");
    }

    private MessageComponent BuildMessageComponents(string? valueSelected = null)
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
            .WithButton("Translate", MessageCommandConstants.TranslateTo.TranslateButtonId)
            .WithButton(
                "Translate & Share",
                MessageCommandConstants.TranslateTo.TranslateAndShareButtonId,
                ButtonStyle.Secondary)
            .Build();
    }
}
