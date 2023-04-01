using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class SlashCommandExecutedHandlerTests
{
    private const string ProviderName = "Test Provider";
    private readonly Mock<DiscordSocketClient> _client;
    private readonly Mock<IMediator> _mediator;
    private readonly SlashCommandExecutedHandler _sut;
    private readonly Mock<ITranslationProvider> _translationProvider;

    public SlashCommandExecutedHandlerTests()
    {
        _mediator = new Mock<IMediator>(MockBehavior.Strict);

        _translationProvider = new Mock<ITranslationProvider>(MockBehavior.Strict);
        _translationProvider.Setup(x => x.ProviderName).Returns(ProviderName);

        _client = new Mock<DiscordSocketClient>(MockBehavior.Strict);

        _sut = new SlashCommandExecutedHandler(
            _mediator.Object,
            new[] { _translationProvider.Object },
            _client.Object,
            Mock.Of<ILogger<SlashCommandExecutedHandler>>()
        );
    }

    [Fact]
    public async Task Handle_SlashCommandExecutedNotification_Success()
    {
        // Arrange
        var data = new Mock<IApplicationCommandInteractionData>(MockBehavior.Strict);
        data.SetupGet(x => x.Name).Returns(CommandConstants.TranslateCommandName);

        var slashCommand = new Mock<ISlashCommandInteraction>(MockBehavior.Strict);
        slashCommand.Setup(x => x.Data).Returns(data.Object);

        var notification = new SlashCommandExecutedNotification { Command = slashCommand.Object };

        _mediator
            .Setup(x => x.Send(It.IsAny<ProcessTranslateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mediator.Verify(
            x => x.Send(It.IsAny<ProcessTranslateCommand>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_RegisterSlashCommands_Returns_IfNoGuildsFound()
    {
        // Arrange
        var command = new RegisterSlashCommands();

        _client.SetupGet(x => x.Guilds).Returns(new List<SocketGuild>());

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _client.VerifyGet(x => x.Guilds, Times.Once);
        _translationProvider.VerifyGet(x => x.TranslateCommandLangCodes, Times.Never);
    }

    [Fact]
    public async Task Handle_RegisterSlashCommands_Success()
    {
        // Arrange
        var guild = new Mock<IGuild>(MockBehavior.Strict);

        guild
            .Setup(
                x =>
                    x.CreateApplicationCommandAsync(
                        It.IsAny<ApplicationCommandProperties>(),
                        It.IsAny<RequestOptions>()
                    )
            )
            .ReturnsAsync(Mock.Of<IApplicationCommand>());

        _translationProvider
            .SetupGet(x => x.TranslateCommandLangCodes)
            .Returns(new HashSet<string>());

        _translationProvider
            .SetupGet(x => x.SupportedLanguages)
            .Returns(
                new HashSet<SupportedLanguage>
                {
                    new() { LangCode = "en", Name = "English" }
                }
            );

        var command = new RegisterSlashCommands { Guild = guild.Object };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        guild.Verify(
            x =>
                x.CreateApplicationCommandAsync(
                    It.IsAny<ApplicationCommandProperties>(),
                    It.IsAny<RequestOptions>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ProcessTranslateCommand_Success()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage { LangCode = "fr", Name = "French" };
        var sourceLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        const string text = "text";

        var data = new Mock<IApplicationCommandInteractionData>(MockBehavior.Strict);
        data.SetupGet(x => x.Options)
            .Returns(
                new List<IApplicationCommandInteractionDataOption>
                {
                    Mock.Of<IApplicationCommandInteractionDataOption>(
                        x =>
                            x.Name == CommandConstants.TranslateCommandToOptionName
                            && (string)x.Value == targetLanguage.LangCode
                    ),
                    Mock.Of<IApplicationCommandInteractionDataOption>(
                        x =>
                            x.Name == CommandConstants.TranslateCommandTextOptionName
                            && (string)x.Value == text
                    ),
                    Mock.Of<IApplicationCommandInteractionDataOption>(
                        x =>
                            x.Name == CommandConstants.TranslateCommandFromOptionName
                            && (string)x.Value == sourceLanguage.LangCode
                    )
                }
            );

        var slashCommand = new Mock<ISlashCommandInteraction>(MockBehavior.Strict);
        slashCommand.Setup(x => x.Data).Returns(data.Object);

        var user = new Mock<IUser>(MockBehavior.Strict);
        user.SetupGet(x => x.Id).Returns(1);
        slashCommand.Setup(x => x.User).Returns(user.Object);

        slashCommand
            .Setup(
                x =>
                    x.RespondAsync(
                        It.IsAny<string>(),
                        It.IsAny<Embed[]>(),
                        It.IsAny<bool>(),
                        It.IsAny<bool>(),
                        It.IsAny<AllowedMentions>(),
                        It.IsAny<MessageComponent>(),
                        It.IsAny<Embed>(),
                        It.IsAny<RequestOptions>()
                    )
            )
            .Returns(Task.CompletedTask);

        _translationProvider
            .SetupGet(x => x.SupportedLanguages)
            .Returns(new HashSet<SupportedLanguage> { sourceLanguage, targetLanguage });

        _translationProvider
            .Setup(
                x =>
                    x.TranslateAsync(
                        It.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                        It.Is<string>(x => x == text),
                        It.IsAny<CancellationToken>(),
                        It.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode)
                    )
            )
            .ReturnsAsync(
                new TranslationResult
                {
                    DetectedLanguageCode = null,
                    DetectedLanguageName = null,
                    TargetLanguageCode = targetLanguage.LangCode,
                    TargetLanguageName = targetLanguage.Name,
                    TranslatedText = "translated text"
                }
            );

        var command = new ProcessTranslateCommand { Command = slashCommand.Object };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _translationProvider.Verify(
            x =>
                x.TranslateAsync(
                    It.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                    It.Is<string>(x => x == text),
                    It.IsAny<CancellationToken>(),
                    It.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode)
                ),
            Times.Once
        );

        slashCommand.Verify(
            x =>
                x.RespondAsync(
                    It.Is<string>(
                        text => text.Contains($"translated text using {ProviderName} from")
                    ),
                    It.IsAny<Embed[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<AllowedMentions>(),
                    It.IsAny<MessageComponent>(),
                    It.IsAny<Embed>(),
                    It.IsAny<RequestOptions>()
                ),
            Times.Once
        );
    }
}
