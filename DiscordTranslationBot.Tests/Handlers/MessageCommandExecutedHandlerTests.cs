using Discord;
using DiscordTranslationBot.Commands.MessageCommandExecuted;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Xunit;
using IMessage = Discord.IMessage;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class MessageCommandExecutedHandlerTests
{
    private const ulong BotUserId = 1UL;
    private readonly IDiscordClient _client;
    private readonly IMediator _mediator;
    private readonly IMessage _message;
    private readonly IMessageCommandInteraction _messageCommand;
    private readonly MessageCommandExecutedHandler _sut;
    private readonly IReadOnlyList<ITranslationProvider> _translationProviders;

    public MessageCommandExecutedHandlerTests()
    {
        _mediator = Substitute.For<IMediator>();

        _translationProviders = new List<ITranslationProvider>
        {
            Substitute.For<ITranslationProvider>(),
            Substitute.For<ITranslationProvider>()
        };

        _client = Substitute.For<IDiscordClient>();
        _client.CurrentUser.Id.Returns(BotUserId);

        _sut = Substitute.ForPartsOf<MessageCommandExecutedHandler>(
            _mediator,
            _translationProviders,
            _client,
            Substitute.For<ILogger<MessageCommandExecutedHandler>>()
        );

        _message = Substitute.For<IMessage>();
        _message.Author.Id.Returns(2UL);
        _message.Channel.Returns(Substitute.For<IMessageChannel>());

        var data = Substitute.For<IMessageCommandInteractionData>();
        data.Message.Returns(_message);

        _messageCommand = Substitute.For<IMessageCommandInteraction>();
        _messageCommand.Data.Returns(data);

        _sut.Configure().GetJumpUrl(_message).Returns(new Uri("http://localhost/test"));
    }

    [Fact]
    public async Task Handle_MessageCommandExecutedNotification_Success()
    {
        // Arrange
        var command = Substitute.For<IMessageCommandInteraction>();
        command.Data.Name.Returns(MessageCommandConstants.TranslateCommandName);

        var notification = new MessageCommandExecutedNotification { Command = command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator
            .Received(1)
            .Send(Arg.Any<ProcessTranslateMessageCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_RegisterMessageCommands_Success(bool isSpecificGuild)
    {
        // Arrange
        IReadOnlyList<IGuild> guilds = isSpecificGuild
            ? new List<IGuild> { Substitute.For<IGuild>() }
            : new List<IGuild> { Substitute.For<IGuild>(), Substitute.For<IGuild>() };

        if (!isSpecificGuild)
        {
            _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns(guilds);
        }

        var command = new RegisterMessageCommands { Guild = isSpecificGuild ? guilds[0] : null };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        if (!isSpecificGuild)
        {
            await _client.Received(1).GetGuildsAsync(options: Arg.Any<RequestOptions>());
        }

        foreach (var guild in guilds)
        {
            await guild
                .Received(1)
                .CreateApplicationCommandAsync(
                    Arg.Any<ApplicationCommandProperties>(),
                    Arg.Any<RequestOptions>()
                );
        }
    }

    [Fact]
    public async Task Handle_RegisterMessageCommands_NoGuilds_Returns()
    {
        // Arrange
        _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns(new List<IGuild>());

        var command = new RegisterMessageCommands();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _client.Received(1).GetGuildsAsync(options: Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_ProcessTranslateMessageCommand_Returns_WhenSanitizedMessageIsEmpty()
    {
        // Arrange
        _message.Content.Returns(string.Empty);

        var command = new ProcessTranslateMessageCommand { Command = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].DidNotReceive().SupportedLanguages;
    }

    [Fact]
    public async Task Handle_ProcessTranslateMessageCommand_Success()
    {
        // Arrange
        _message.Content.Returns("text");
        _messageCommand.UserLocale.Returns("en-US");

        var supportedLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        _translationProviders[0].SupportedLanguages.Returns(
            new HashSet<SupportedLanguage> { supportedLanguage }
        );

        _translationProviders[0]
            .TranslateAsync(
                Arg.Any<SupportedLanguage>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = "fr",
                    DetectedLanguageName = "French",
                    TargetLanguageCode = supportedLanguage.LangCode,
                    TargetLanguageName = supportedLanguage.Name,
                    TranslatedText = "translated text"
                }
            );

        var command = new ProcessTranslateMessageCommand { Command = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _messageCommand
            .Received(1)
            .RespondAsync(
                embed: Arg.Is<Embed>(x => x.Title == "Translated Message"),
                ephemeral: true,
                options: Arg.Any<RequestOptions>()
            );
    }

    [Fact]
    public async Task Handle_ProcessTranslateMessageCommand_UsesNextTranslationProvider_Success()
    {
        // Arrange
        _message.Content.Returns("text");
        _messageCommand.UserLocale.Returns("en");

        var supportedLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        _translationProviders[0].SupportedLanguages.Returns(new HashSet<SupportedLanguage>());

        _translationProviders[0]
            .TranslateAsync(
                Arg.Any<SupportedLanguage>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new InvalidOperationException("test"));

        _translationProviders[1].SupportedLanguages.Returns(
            new HashSet<SupportedLanguage> { supportedLanguage }
        );

        _translationProviders[1]
            .TranslateAsync(
                Arg.Any<SupportedLanguage>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = "fr",
                    DetectedLanguageName = "French",
                    TargetLanguageCode = supportedLanguage.LangCode,
                    TargetLanguageName = supportedLanguage.Name,
                    TranslatedText = "translated text"
                }
            );

        var command = new ProcessTranslateMessageCommand { Command = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(1).SupportedLanguages;
        _ = _translationProviders[1].Received(1).SupportedLanguages;

        await _messageCommand
            .Received(1)
            .RespondAsync(
                embed: Arg.Is<Embed>(x => x.Title == "Translated Message"),
                ephemeral: true,
                options: Arg.Any<RequestOptions>()
            );
    }

    [Fact]
    public async Task Handle_ProcessTranslateMessageCommand_Returns_WhenTranslatingBotMessage()
    {
        // Arrange
        _message.Author.Id.Returns(BotUserId);

        var command = new ProcessTranslateMessageCommand { Command = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _messageCommand
            .Received(1)
            .RespondAsync(
                "Translating this bot's messages isn't allowed.",
                ephemeral: true,
                options: Arg.Any<RequestOptions>()
            );
    }

    [Fact]
    public async Task Handle_ProcessTranslateMessageCommand_Returns_IfNoProviderSupportsLocale()
    {
        // Arrange
        _message.Content.Returns("text");

        const string userLocale = "en-US";
        _messageCommand.UserLocale.Returns(userLocale);

        var supportedLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        foreach (var translationProvider in _translationProviders)
        {
            translationProvider.SupportedLanguages.Returns(
                new HashSet<SupportedLanguage> { supportedLanguage }
            );

            translationProvider
                .TranslateAsync(
                    Arg.Any<SupportedLanguage>(),
                    Arg.Any<string>(),
                    Arg.Any<CancellationToken>()
                )
                .ThrowsAsync(new InvalidOperationException("test"));
        }

        var command = new ProcessTranslateMessageCommand { Command = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _messageCommand
            .Received(1)
            .RespondAsync(
                $"Your locale {userLocale} isn't supported for translation via this action.",
                ephemeral: true,
                options: Arg.Any<RequestOptions>()
            );
    }

    [Fact]
    public async Task Handle_ProcessTranslateMessageCommand_Returns_WhenTranslatedTextIsSame()
    {
        // Arrange
        const string text = "text";

        _message.Content.Returns(text);
        _messageCommand.UserLocale.Returns("en-US");

        var supportedLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        _translationProviders[0].SupportedLanguages.Returns(
            new HashSet<SupportedLanguage> { supportedLanguage }
        );

        _translationProviders[0]
            .TranslateAsync(
                Arg.Any<SupportedLanguage>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = "fr",
                    DetectedLanguageName = "French",
                    TargetLanguageCode = supportedLanguage.LangCode,
                    TargetLanguageName = supportedLanguage.Name,
                    TranslatedText = text
                }
            );

        var command = new ProcessTranslateMessageCommand { Command = _messageCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProviders[0].Received(2).SupportedLanguages;

        await _messageCommand
            .Received(1)
            .RespondAsync(
                "The message couldn't be translated. It might already be in your language or the translator failed to detect its source language.",
                ephemeral: true,
                options: Arg.Any<RequestOptions>()
            );
    }
}
