using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Notifications.Handlers;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Handlers;

public sealed class TranslateToMessageCommandHandlerTests
{
    private readonly IMessageHelper _messageHelper;
    private readonly TranslateToMessageCommandHandler _sut;
    private readonly ITranslationProvider _translationProvider;

    public TranslateToMessageCommandHandlerTests()
    {
        _translationProvider = Substitute.For<ITranslationProvider>();

        var translationProviderFactory = Substitute.For<ITranslationProviderFactory>();
        translationProviderFactory.PrimaryProvider.Returns(_translationProvider);

        _messageHelper = Substitute.For<IMessageHelper>();

        _sut = new TranslateToMessageCommandHandler(
            translationProviderFactory,
            _messageHelper,
            new LoggerFake<TranslateToMessageCommandHandler>());
    }

    [Test]
    public async Task Handle_ButtonExecutedNotification_ReturnsIfIncorrectButtonIds(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new ButtonExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns("incorrect_button_id");

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await notification.Interaction.DidNotReceiveWithAnyArgs().DeferAsync();
    }

    [Test]
    [Arguments(MessageCommandConstants.TranslateTo.TranslateButtonId)]
    [Arguments(MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)]
    public async Task Handle_ButtonExecutedNotification_EmptySourceText(
        string buttonId,
        CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new ButtonExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns(buttonId);

        notification.Interaction.Message.Reference.Returns(new MessageReference(1UL));

        _messageHelper
        .GetJumpUrlsInMessage(Arg.Any<IMessage>())
        .Returns(
        [
            new JumpUrl
            {
                IsDmChannel = false,
                GuildId = 2UL,
                ChannelId = 3UL,
                MessageId = 4UL
            }
        ]);

        var referencedMessage = Substitute.For<IMessage>();
        referencedMessage.Content.Returns(string.Empty);

        notification
            .Interaction
            .Message
            .Channel
            .GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>())
            .Returns(referencedMessage);

        var receivedProperties = new MessageProperties();

        notification
            .Interaction
            .When(x => x.ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>()))
            .Do(x => x.Arg<Action<MessageProperties>>().Invoke(receivedProperties));

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>());

        receivedProperties.Content.Value.ShouldBe("No text to translate.");
        receivedProperties.Components.Value.ShouldBeNull();
    }

    [Test]
    [Arguments(MessageCommandConstants.TranslateTo.TranslateButtonId)]
    [Arguments(MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)]
    public async Task Handle_ButtonExecutedNotification_FailureToDetectSourceLanguage(
        string buttonId,
        CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new ButtonExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns(buttonId);
        notification.Interaction.Message.Reference.Returns(new MessageReference(1UL));

        _messageHelper
        .GetJumpUrlsInMessage(Arg.Any<IMessage>())
        .Returns(
        [
            new JumpUrl
            {
                IsDmChannel = false,
                GuildId = 2UL,
                ChannelId = 3UL,
                MessageId = 4UL
            }
        ]);

        var referencedMessage = Substitute.For<IMessage>();
        const string referencedMessageContent = "test";
        referencedMessage.Content.Returns(referencedMessageContent);

        notification
            .Interaction
            .Message
            .Channel
            .GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>())
            .Returns(referencedMessage);

        var receivedProperties = new MessageProperties();

        notification
            .Interaction
            .When(x => x.ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>()))
            .Do(x => x.Arg<Action<MessageProperties>>().Invoke(receivedProperties));

        const string selectedLanguageCode = "en-US";

        notification.Interaction.Message.Components.Returns(
        [
            new ActionRowBuilder()
                .WithSelectMenu(
                    MessageCommandConstants.TranslateTo.SelectMenuId,
                    [new SelectMenuOptionBuilder().WithLabel("test").WithValue(selectedLanguageCode).WithDefault(true)])
                .Build()
        ]);

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage>
            {
                new()
                {
                    LangCode = selectedLanguageCode,
                    Name = "English"
                }
            });

        _translationProvider
            .TranslateAsync(default!, default!, Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    TargetLanguageCode = selectedLanguageCode,
                    TranslatedText = referencedMessageContent
                });

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>());

        receivedProperties.Content.Value.ShouldBe(
            "Couldn't detect the source language to translate from or the result is the same.");

        receivedProperties.Components.Value.ShouldBeNull();
    }

    [Test]
    [Arguments(MessageCommandConstants.TranslateTo.TranslateButtonId)]
    [Arguments(MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)]
    public async Task Handle_ButtonExecutedNotification_Success(string buttonId, CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new ButtonExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns(buttonId);
        notification.Interaction.Message.Reference.Returns(new MessageReference(1UL));

        const ulong referencedMessageId = 5UL;

        _messageHelper
        .GetJumpUrlsInMessage(Arg.Any<IMessage>())
        .Returns(
        [
            new JumpUrl
            {
                IsDmChannel = false,
                GuildId = 2UL,
                ChannelId = 3UL,
                MessageId = referencedMessageId
            }
        ]);

        var referencedMessage = Substitute.For<IMessage>();
        referencedMessage.Content.Returns("test");

        notification
            .Interaction
            .Message
            .Channel
            .GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>())
            .Returns(referencedMessage);

        const string translationReplytext = "result";
        _messageHelper
            .BuildTranslationReplyWithReference(Arg.Any<IMessage>(), Arg.Any<TranslationResult>(), Arg.Any<ulong?>())
            .Returns(translationReplytext);

        MessageProperties? receivedProperties = null;
        string? receivedSentMessagetext = null;
        ulong? receivedReferencedMessageId = null;

        if (buttonId == MessageCommandConstants.TranslateTo.TranslateButtonId)
        {
            notification
                .Interaction
                .When(
                    x => x.ModifyOriginalResponseAsync(
                        Arg.Any<Action<MessageProperties>>(),
                        Arg.Any<RequestOptions?>()))
                .Do(
                    x =>
                    {
                        receivedProperties ??= new MessageProperties();
                        x.Arg<Action<MessageProperties>>().Invoke(receivedProperties);
                    });
        }
        else if (buttonId == MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)
        {
            notification
                .Interaction
                .Message
                .Channel
                .WhenForAnyArgs(x => x.SendMessageAsync())
                .Do(
                    x =>
                    {
                        receivedSentMessagetext = x.ArgAt<string>(0);
                        receivedReferencedMessageId = x.Arg<MessageReference>().MessageId.Value;
                    });
        }

        const string selectedLanguageCode = "en-US";

        notification.Interaction.Message.Components.Returns(
        [
            new ActionRowBuilder()
                .WithSelectMenu(
                    MessageCommandConstants.TranslateTo.SelectMenuId,
                    [new SelectMenuOptionBuilder().WithLabel("test").WithValue(selectedLanguageCode).WithDefault(true)])
                .Build()
        ]);

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage>
            {
                new()
                {
                    LangCode = selectedLanguageCode,
                    Name = "English"
                }
            });

        _translationProvider
            .TranslateAsync(default!, default!, Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    TargetLanguageCode = selectedLanguageCode,
                    TranslatedText = "translated text"
                });

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        if (buttonId == MessageCommandConstants.TranslateTo.TranslateButtonId)
        {
            await notification
                .Interaction
                .Received(1)
                .ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>());

            receivedProperties!.Content.Value.ShouldBe(translationReplytext);
            receivedProperties.Components.Value.ShouldBeNull();
        }
        else if (buttonId == MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)
        {
            await notification.Interaction.Received(1).DeleteOriginalResponseAsync(Arg.Any<RequestOptions?>());
            await notification.Interaction.Message.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();
            receivedSentMessagetext.ShouldBe(translationReplytext);
            receivedReferencedMessageId.ShouldBe(referencedMessageId);
        }
    }

    [Test]
    public async Task Handle_MessageCommandExecutedNotification_ReturnsIfIncorrectCommandName(
        CancellationToken cancellationToken)
    {
        // Arrange
        var notification =
            new MessageCommandExecutedNotification { Interaction = Substitute.For<IMessageCommandInteraction>() };

        notification.Interaction.Data.Name.Returns("incorrect_message_command_name");

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await notification.Interaction.DidNotReceiveWithAnyArgs().RespondAsync();
    }

    [Test]
    public async Task Handle_MessageCommandExecutedNotification_EmptySourceText(CancellationToken cancellationToken)
    {
        // Arrange
        var notification =
            new MessageCommandExecutedNotification { Interaction = Substitute.For<IMessageCommandInteraction>() };

        notification.Interaction.Data.Name.Returns(MessageCommandConstants.TranslateTo.CommandName);

        notification.Interaction.Data.Message.Content.Returns(" ");

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .RespondAsync("No text to translate.", ephemeral: true, options: Arg.Any<RequestOptions?>());
    }

    [Test]
    public async Task Handle_MessageCommandExecutedNotification_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var notification =
            new MessageCommandExecutedNotification { Interaction = Substitute.For<IMessageCommandInteraction>() };

        notification.Interaction.Data.Name.Returns(MessageCommandConstants.TranslateTo.CommandName);
        notification.Interaction.Data.Message.Content.Returns("text");

        _messageHelper.GetJumpUrl(Arg.Any<IMessage>()).Returns(new Uri("http://localhost", UriKind.Absolute));

        MessageComponent? messageComponents = null;

        notification
            .Interaction
            .WhenForAnyArgs(x => x.RespondAsync(components: Arg.Any<MessageComponent>()))
            .Do(x => messageComponents = x.Arg<MessageComponent>());

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .RespondAsync(
                Arg.Is<string>(x => x.StartsWith("What would you like to translate")),
                components: Arg.Any<MessageComponent>(),
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());

        messageComponents!.Components.Count.ShouldBe(2);

        var firstRowComponents = messageComponents.Components.ElementAt(0).Components;
        firstRowComponents.Count.ShouldBe(1);
        firstRowComponents.ShouldAllBe(x => x is SelectMenuComponent);

        var lastRowComponents = messageComponents.Components.ElementAt(1).Components;
        lastRowComponents.Count.ShouldBe(2);
        lastRowComponents.ShouldAllBe(x => x is ButtonComponent);
        ((ButtonComponent)lastRowComponents.ElementAt(0)).IsDisabled.ShouldBeTrue();
        ((ButtonComponent)lastRowComponents.ElementAt(1)).IsDisabled.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_SelectMenuExecutedNotification_ReturnsIfIncorrectSelectMenuId(
        CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new SelectMenuExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns("incorrect_select_menu_id");

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await notification.Interaction.DidNotReceiveWithAnyArgs().UpdateAsync(default);
    }

    [Test]
    public async Task Handle_SelectMenuExecutedNotification_UpdatesExpected(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new SelectMenuExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns(MessageCommandConstants.TranslateTo.SelectMenuId);
        notification.Interaction.Data.Values.Returns(["selected_value"]);

        var receivedProperties = new MessageProperties();

        notification
            .Interaction
            .When(x => x.UpdateAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>()))
            .Do(x => x.Arg<Action<MessageProperties>>().Invoke(receivedProperties));

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        receivedProperties.Components.Value.Components.Count.ShouldBe(2);

        var firstRowComponents = receivedProperties.Components.Value.Components.ElementAt(0).Components;
        firstRowComponents.Count.ShouldBe(1);
        firstRowComponents.ShouldAllBe(x => x is SelectMenuComponent);

        var lastRowComponents = receivedProperties.Components.Value.Components.ElementAt(1).Components;
        lastRowComponents.Count.ShouldBe(2);
        lastRowComponents.ShouldAllBe(x => x is ButtonComponent);
        ((ButtonComponent)lastRowComponents.ElementAt(0)).IsDisabled.ShouldBeFalse();
        ((ButtonComponent)lastRowComponents.ElementAt(1)).IsDisabled.ShouldBeFalse();
    }
}
