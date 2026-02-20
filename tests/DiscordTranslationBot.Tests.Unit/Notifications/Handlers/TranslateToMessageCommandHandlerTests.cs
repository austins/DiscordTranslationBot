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

    [Fact]
    public async Task Handle_ButtonExecutedNotification_ReturnsIfIncorrectButtonIds()
    {
        // Arrange
        var notification = new ButtonExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns("incorrect_button_id");

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification.Interaction.DidNotReceiveWithAnyArgs().DeferAsync();
    }

    [Theory]
    [InlineData(MessageCommandConstants.TranslateTo.TranslateButtonId)]
    [InlineData(MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)]
    public async Task Handle_ButtonExecutedNotification_EmptySourceText(string buttonId)
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
            .Interaction.Message.Channel.GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>())
            .Returns(referencedMessage);

        var receivedProperties = new MessageProperties();

        notification
            .Interaction
            .When(x => x.ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>()))
            .Do(x => x.Arg<Action<MessageProperties>>().Invoke(receivedProperties));

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction.Received(1)
            .ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>());

        receivedProperties.Content.Value.Should().Be("No text to translate.");
        receivedProperties.Components.Value.Should().BeNull();
    }

    [Theory]
    [InlineData(MessageCommandConstants.TranslateTo.TranslateButtonId)]
    [InlineData(MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)]
    public async Task Handle_ButtonExecutedNotification_FailureToDetectSourceLanguage(string buttonId)
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
            .Interaction.Message.Channel.GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>())
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
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    TargetLanguageCode = selectedLanguageCode,
                    TranslatedText = referencedMessageContent
                });

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction.Received(1)
            .ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>());

        receivedProperties
            .Content.Value.Should()
            .Be("Couldn't detect the source language to translate from or the result is the same.");

        receivedProperties.Components.Value.Should().BeNull();
    }

    [Theory]
    [InlineData(MessageCommandConstants.TranslateTo.TranslateButtonId)]
    [InlineData(MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)]
    public async Task Handle_ButtonExecutedNotification_Success(string buttonId)
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
            .Interaction.Message.Channel.GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>())
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
                .Interaction.When(x => x.ModifyOriginalResponseAsync(
                    Arg.Any<Action<MessageProperties>>(),
                    Arg.Any<RequestOptions?>()))
                .Do(x =>
                {
                    receivedProperties ??= new MessageProperties();
                    x.Arg<Action<MessageProperties>>().Invoke(receivedProperties);
                });
        }
        else if (buttonId == MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)
        {
            notification
                .Interaction.Message.Channel.WhenForAnyArgs(x => x.SendMessageAsync())
                .Do(x =>
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
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    TargetLanguageCode = selectedLanguageCode,
                    TranslatedText = "translated text"
                });

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        if (buttonId == MessageCommandConstants.TranslateTo.TranslateButtonId)
        {
            await notification
                .Interaction.Received(1)
                .ModifyOriginalResponseAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>());

            receivedProperties!.Content.Value.Should().Be(translationReplytext);
            receivedProperties.Components.Value.Should().BeNull();
        }
        else if (buttonId == MessageCommandConstants.TranslateTo.TranslateAndShareButtonId)
        {
            await notification.Interaction.Received(1).DeleteOriginalResponseAsync(Arg.Any<RequestOptions?>());
            await notification.Interaction.Message.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();
            receivedSentMessagetext.Should().Be(translationReplytext);
            receivedReferencedMessageId.Should().Be(referencedMessageId);
        }
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_ReturnsIfIncorrectCommandName()
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification
            { Interaction = Substitute.For<IMessageCommandInteraction>() };

        notification.Interaction.Data.Name.Returns("incorrect_message_command_name");

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification.Interaction.DidNotReceiveWithAnyArgs().RespondAsync();
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_EmptySourceText()
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification
            { Interaction = Substitute.For<IMessageCommandInteraction>() };

        notification.Interaction.Data.Name.Returns(MessageCommandConstants.TranslateTo.CommandName);

        notification.Interaction.Data.Message.Content.Returns(" ");

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction.Received(1)
            .RespondAsync("No text to translate.", ephemeral: true, options: Arg.Any<RequestOptions?>());
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Success()
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification
            { Interaction = Substitute.For<IMessageCommandInteraction>() };

        notification.Interaction.Data.Name.Returns(MessageCommandConstants.TranslateTo.CommandName);
        notification.Interaction.Data.Message.Content.Returns("text");

        _messageHelper.GetJumpUrl(Arg.Any<IMessage>()).Returns(new Uri("http://localhost", UriKind.Absolute));

        MessageComponent? messageComponents = null;

        notification
            .Interaction.WhenForAnyArgs(x => x.RespondAsync())
            .Do(x => messageComponents = x.Arg<MessageComponent>());

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction.Received(1)
            .RespondAsync(
                Arg.Is<string>(x => x.StartsWith("What would you like to translate")),
                components: Arg.Any<MessageComponent>(),
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());

        messageComponents!.Components.Count.Should().Be(2);

        var firstRow = messageComponents.Components.ElementAt(0) as ActionRowComponent;
        firstRow.Should().NotBeNull();
        firstRow.Components.Count.Should().Be(1);
        firstRow.Components.Should().AllBeOfType<SelectMenuComponent>();

        var lastRow = messageComponents.Components.ElementAt(1) as ActionRowComponent;
        lastRow.Should().NotBeNull();
        lastRow.Components.Count.Should().Be(2);
        lastRow.Components.Should().AllBeOfType<ButtonComponent>();
        ((ButtonComponent)lastRow.Components.ElementAt(0)).IsDisabled.Should().BeTrue();
        ((ButtonComponent)lastRow.Components.ElementAt(1)).IsDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SelectMenuExecutedNotification_ReturnsIfIncorrectSelectMenuId()
    {
        // Arrange
        var notification = new SelectMenuExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns("incorrect_select_menu_id");

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification.Interaction.DidNotReceiveWithAnyArgs().UpdateAsync(default);
    }

    [Fact]
    public async Task Handle_SelectMenuExecutedNotification_UpdatesExpected()
    {
        // Arrange
        var notification = new SelectMenuExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };
        notification.Interaction.Data.CustomId.Returns(MessageCommandConstants.TranslateTo.SelectMenuId);
        notification.Interaction.Data.Values.Returns(["selected_value"]);

        var receivedProperties = new MessageProperties();

        notification
            .Interaction.When(x => x.UpdateAsync(Arg.Any<Action<MessageProperties>>(), Arg.Any<RequestOptions?>()))
            .Do(x => x.Arg<Action<MessageProperties>>().Invoke(receivedProperties));

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        receivedProperties.Components.Value.Components.Count.Should().Be(2);

        var firstRow = receivedProperties.Components.Value.Components.ElementAt(0) as ActionRowComponent;
        firstRow.Should().NotBeNull();
        firstRow.Components.Count.Should().Be(1);
        firstRow.Components.Should().AllBeOfType<SelectMenuComponent>();

        var lastRow = receivedProperties.Components.Value.Components.ElementAt(1) as ActionRowComponent;
        lastRow.Should().NotBeNull();
        lastRow.Components.Count.Should().Be(2);
        lastRow.Components.Should().AllBeOfType<ButtonComponent>();
        ((ButtonComponent)lastRow.Components.ElementAt(0)).IsDisabled.Should().BeFalse();
        ((ButtonComponent)lastRow.Components.ElementAt(1)).IsDisabled.Should().BeFalse();
    }
}
