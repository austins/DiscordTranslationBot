using Discord;
using Discord.Net;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Notifications.Handlers;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Services;
using System.Collections.Frozen;
using System.Net;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Handlers;

public sealed class TranslateToMessageCommandHandlerTests
{
    private const ulong BotUserId = 1UL;
    private const ulong ChannelId = 2UL;
    private const ulong ReferencedMessageId = 3UL;
    private const ulong UserId = 4UL;
    private const string SelectedLanguageCode = "en-US";

    private readonly IMessageChannel _channel;
    private readonly IDiscordClient _client;
    private readonly IMessageHelper _messageHelper;
    private readonly ITranslationRateLimiter _rateLimiter;
    private readonly IMessage _referencedMessage;
    private readonly TranslateToMessageCommandHandler _sut;
    private readonly ITranslationProvider _translationProvider;
    private readonly ITranslationProviderFactory _translationProviderFactory;

    public TranslateToMessageCommandHandlerTests()
    {
        _referencedMessage = Substitute.For<IMessage>();
        _referencedMessage.Content.Returns("text");

        _channel = Substitute.For<IMessageChannel>();
        _channel
            .GetMessageAsync(ReferencedMessageId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions?>())
            .Returns(_referencedMessage);

        _client = Substitute.For<IDiscordClient>();
        _client.CurrentUser.Id.Returns(BotUserId);
        _client.GetChannelAsync(ChannelId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions?>()).Returns(_channel);

        _translationProvider = Substitute.For<ITranslationProvider>();
        _translationProvider.SupportedLanguages.Returns(
            new Dictionary<string, string> { { SelectedLanguageCode, "English" } }.ToFrozenDictionary());

        _translationProviderFactory = Substitute.For<ITranslationProviderFactory>();
        _translationProviderFactory.PrimaryProvider.Returns(_translationProvider);

        _messageHelper = Substitute.For<IMessageHelper>();

        _rateLimiter = Substitute.For<ITranslationRateLimiter>();
        _rateLimiter.TryAcquire(Arg.Any<ulong>()).Returns(true);

        _sut = new TranslateToMessageCommandHandler(
            _client,
            _translationProviderFactory,
            _messageHelper,
            _rateLimiter,
            new LoggerFake<TranslateToMessageCommandHandler>());
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_ReturnsIfIncorrectCommandName()
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification
        {
            Interaction = Substitute.For<IMessageCommandInteraction>()
        };

        notification.Interaction.Data.Name.Returns("incorrect_message_command_name");

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification.Interaction.DidNotReceiveWithAnyArgs().RespondAsync();
        await notification.Interaction.DidNotReceiveWithAnyArgs().RespondWithModalAsync(default!);
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification
        {
            Interaction = Substitute.For<IMessageCommandInteraction>()
        };

        notification.Interaction.Data.Name.Returns(MessageCommandConstants.TranslateTo.CommandName);
        notification.Interaction.Data.Message.Author.Id.Returns(BotUserId);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .RespondAsync(
                $"{NeoSmart.Unicode.Emoji.NoEntry} Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());

        await notification.Interaction.DidNotReceiveWithAnyArgs().RespondWithModalAsync(default!);
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_EmptySourceText()
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification
        {
            Interaction = Substitute.For<IMessageCommandInteraction>()
        };

        notification.Interaction.Data.Name.Returns(MessageCommandConstants.TranslateTo.CommandName);
        notification.Interaction.Data.Message.Content.Returns(" ");

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .RespondAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} No text to translate.",
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());

        await notification.Interaction.DidNotReceiveWithAnyArgs().RespondWithModalAsync(default!);
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_RespondsWithModal()
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification
        {
            Interaction = Substitute.For<IMessageCommandInteraction>()
        };

        notification.Interaction.Data.Name.Returns(MessageCommandConstants.TranslateTo.CommandName);
        notification.Interaction.Data.Message.Content.Returns("text");
        notification.Interaction.Data.Message.Id.Returns(ReferencedMessageId);

        _translationProviderFactory.GetSupportedLanguagesForOptions().Returns([new SupportedLanguage("en", "English")]);

        Modal? modal = null;

        notification.Interaction.WhenForAnyArgs(x => x.RespondWithModalAsync(default!)).Do(x => modal = x.Arg<Modal>());

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification.Interaction.ReceivedWithAnyArgs(1).RespondWithModalAsync(default!);

        modal.Should().NotBeNull();
        modal.CustomId.Should().Be($"{MessageCommandConstants.TranslateTo.ModalIdPrefix}{ReferencedMessageId}");

        var labels = modal.Component.Components.Cast<LabelComponent>().ToList();
        labels.Should().HaveCount(2);

        var languageSelect = labels[0].Component.Should().BeOfType<SelectMenuComponent>().Subject;
        languageSelect.CustomId.Should().Be(MessageCommandConstants.TranslateTo.LanguageSelectMenuId);
        languageSelect.Options.Should().ContainSingle(o => o.Label == "English" && o.Value == "en");

        var shareCheckbox = labels[1].Component.Should().BeOfType<CheckboxComponent>().Subject;
        shareCheckbox.CustomId.Should().Be(MessageCommandConstants.TranslateTo.ShareCheckboxId);
        shareCheckbox.DefaultState.Should().BeFalse();
    }

    [Theory]
    [InlineData("incorrect_modal_id")]
    [InlineData($"{MessageCommandConstants.TranslateTo.ModalIdPrefix}not_a_message_id")]
    public async Task Handle_ModalSubmittedNotification_ReturnsIfIncorrectCustomId(string customId)
    {
        // Arrange
        var notification = new ModalSubmittedNotification { Interaction = Substitute.For<IModalInteraction>() };
        notification.Interaction.Data.CustomId.Returns(customId);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _rateLimiter.DidNotReceiveWithAnyArgs().TryAcquire(default);
        await notification.Interaction.DidNotReceiveWithAnyArgs().DeferAsync();
    }

    [Fact]
    public async Task Handle_ModalSubmittedNotification_Returns_WhenRateLimited()
    {
        // Arrange
        var notification = CreateModalSubmittedNotification();

        _rateLimiter.TryAcquire(UserId).Returns(false);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .RespondAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} {ITranslationRateLimiter.RateLimitedMessage}",
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());

        await notification.Interaction.DidNotReceiveWithAnyArgs().DeferAsync();

        await _translationProvider
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ModalSubmittedNotification_ReferencedMessageDeleted()
    {
        // Arrange
        var notification = CreateModalSubmittedNotification();

        _channel
            .GetMessageAsync(ReferencedMessageId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions?>())
            .Returns((IMessage?)null);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification.Interaction.Received(1).DeferAsync(true, Arg.Any<RequestOptions?>());

        await notification
            .Interaction
            .Received(1)
            .FollowupAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} The original message was deleted.",
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());

        await _translationProvider
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ModalSubmittedNotification_MissingPermissions()
    {
        // Arrange
        var notification = CreateModalSubmittedNotification();

        _channel
            .GetMessageAsync(ReferencedMessageId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions?>())
            .ThrowsAsync(new HttpException(HttpStatusCode.Forbidden, Substitute.For<IRequest>()));

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .FollowupAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} The bot is missing permissions in this channel.",
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());

        await _translationProvider
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ModalSubmittedNotification_EmptySourceText()
    {
        // Arrange
        var notification = CreateModalSubmittedNotification();

        _referencedMessage.Content.Returns(" ");

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .FollowupAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} No text to translate.",
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());

        await _translationProvider
            .DidNotReceiveWithAnyArgs()
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ModalSubmittedNotification_FailureToDetectSourceLanguageOrResultIsSame()
    {
        // Arrange
        var notification = CreateModalSubmittedNotification();

        _translationProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    TargetLanguageCode = SelectedLanguageCode,
                    TranslatedText = "text"
                });

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .FollowupAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} Couldn't detect the source language to translate from or the result is the same.",
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());
    }

    [Fact]
    public async Task Handle_ModalSubmittedNotification_Returns_WhenExceptionThrown()
    {
        // Arrange
        var notification = CreateModalSubmittedNotification();

        _translationProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ThrowsAsyncForAnyArgs<InvalidOperationException>();

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await notification
            .Interaction
            .Received(1)
            .FollowupAsync(
                $"{NeoSmart.Unicode.Emoji.Warning} Failed to translate text. Please try again.",
                ephemeral: true,
                options: Arg.Any<RequestOptions?>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_ModalSubmittedNotification_Success(bool share)
    {
        // Arrange
        var notification = CreateModalSubmittedNotification(share);

        _translationProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(
                new TranslationResult
                {
                    TargetLanguageCode = SelectedLanguageCode,
                    TranslatedText = "translated text"
                });

        const string translationReplyText = "result";
        _messageHelper
            .BuildTranslationReplyWithReference(_referencedMessage, Arg.Any<TranslationResult>(), Arg.Any<ulong?>())
            .Returns(translationReplyText);

        string? receivedSentMessageText = null;
        AllowedMentions? receivedAllowedMentions = null;
        ulong? receivedReferencedMessageId = null;

        _referencedMessage
            .Channel
            .WhenForAnyArgs(x => x.SendMessageAsync())
            .Do(x =>
            {
                receivedSentMessageText = x.ArgAt<string>(0);
                receivedAllowedMentions = x.Arg<AllowedMentions>();
                receivedReferencedMessageId = x.Arg<MessageReference>().MessageId.Value;
            });

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == SelectedLanguageCode),
                "text",
                TestContext.Current.CancellationToken);

        _messageHelper
            .Received(1)
            .BuildTranslationReplyWithReference(
                _referencedMessage,
                Arg.Any<TranslationResult>(),
                share ? UserId : null);

        if (share)
        {
            await notification.Interaction.Received(1).DeleteOriginalResponseAsync(Arg.Any<RequestOptions?>());
            await _referencedMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();
            receivedSentMessageText.Should().Be(translationReplyText);
            receivedAllowedMentions.Should().BeSameAs(AllowedMentions.None);
            receivedReferencedMessageId.Should().Be(ReferencedMessageId);
        }
        else
        {
            await notification
                .Interaction
                .Received(1)
                .FollowupAsync(translationReplyText, ephemeral: true, options: Arg.Any<RequestOptions?>());

            await _referencedMessage.Channel.DidNotReceiveWithAnyArgs().SendMessageAsync();
        }
    }

    private static ModalSubmittedNotification CreateModalSubmittedNotification(bool share = false)
    {
        var notification = new ModalSubmittedNotification { Interaction = Substitute.For<IModalInteraction>() };
        notification.Interaction.Data.CustomId.Returns(
            $"{MessageCommandConstants.TranslateTo.ModalIdPrefix}{ReferencedMessageId}");

        notification.Interaction.ChannelId.Returns(ChannelId);
        notification.Interaction.User.Id.Returns(UserId);

        var languageSelect = Substitute.For<IComponentInteractionData>();
        languageSelect.CustomId.Returns(MessageCommandConstants.TranslateTo.LanguageSelectMenuId);
        languageSelect.Values.Returns([SelectedLanguageCode]);

        var shareCheckbox = Substitute.For<IComponentInteractionData>();
        shareCheckbox.CustomId.Returns(MessageCommandConstants.TranslateTo.ShareCheckboxId);
        shareCheckbox.BoolValue.Returns(share);

        notification.Interaction.Data.Components.Returns([languageSelect, shareCheckbox]);

        return notification;
    }
}
