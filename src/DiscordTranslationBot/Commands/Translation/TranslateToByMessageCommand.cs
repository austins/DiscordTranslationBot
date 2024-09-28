using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Discord.Services;
using DiscordTranslationBot.Providers.Translation;
using Humanizer;

namespace DiscordTranslationBot.Commands.Translation;

public sealed class TranslateToByMessageCommand : ICommand
{
    /// <summary>
    /// The message command.
    /// </summary>
    [Required]
    public required IMessageCommandInteraction MessageCommand { get; init; }
}

public sealed class TranslateToByMessageCommandHandler
    : ICommandHandler<TranslateToByMessageCommand>,
        INotificationHandler<MessageCommandExecutedEvent>,
        INotificationHandler<SelectMenuExecutedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMessageHelper _messageHelper;
    private readonly TranslationProviderFactory _translationProviderFactory;

    public TranslateToByMessageCommandHandler(
        TranslationProviderFactory translationProviderFactory,
        IMessageHelper messageHelper,
        IMediator mediator)
    {
        _translationProviderFactory = translationProviderFactory;
        _messageHelper = messageHelper;
        _mediator = mediator;
    }

    public async ValueTask<Unit> Handle(TranslateToByMessageCommand command, CancellationToken cancellationToken)
    {
        await command.MessageCommand.DeferAsync(true, new RequestOptions { CancelToken = cancellationToken });

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

        await command.MessageCommand.FollowupAsync(
            $"What would you like to translate {_messageHelper.GetJumpUrl(command.MessageCommand.Data.Message)} to?",
            components: components,
            ephemeral: true,
            options: new RequestOptions { CancelToken = cancellationToken });

        return Unit.Value;
    }

    public async ValueTask Handle(MessageCommandExecutedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.MessageCommand.Data.Name != MessageCommandConstants.TranslateTo.CommandName)
        {
            return;
        }

        await _mediator.Send(
            new TranslateToByMessageCommand { MessageCommand = notification.MessageCommand },
            cancellationToken);
    }

    public async ValueTask Handle(SelectMenuExecutedEvent notification, CancellationToken cancellationToken)
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
