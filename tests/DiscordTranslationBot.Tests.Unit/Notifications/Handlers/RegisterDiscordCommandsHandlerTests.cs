using Discord;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Notifications.Handlers;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Handlers;

public sealed class RegisterDiscordCommandsHandlerTests
{
    private const int ExpectedMessageCommandCount = 2;
    private const int ExpectedSlashCommandCount = 1;
    private readonly IDiscordClient _client;
    private readonly RegisterDiscordCommandsHandler _sut;
    private readonly ITranslationProvider _translationProvider;

    public RegisterDiscordCommandsHandlerTests()
    {
        _client = Substitute.For<IDiscordClient>();

        _translationProvider = Substitute.For<ITranslationProvider>();
        _translationProvider.TranslateCommandLangCodes.Returns(new HashSet<string>());

        var supportedLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        _translationProvider.SupportedLanguages.Returns(new HashSet<SupportedLanguage> { supportedLanguage });

        var translationProviderFactory = Substitute.For<ITranslationProviderFactory>();
        translationProviderFactory.GetSupportedLanguagesForOptions().Returns([supportedLanguage]);

        _sut = new RegisterDiscordCommandsHandler(
            _client,
            translationProviderFactory,
            new LoggerFake<RegisterDiscordCommandsHandler>());
    }

    [Test]
    public async Task Handle_JoinedGuildNotification_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new JoinedGuildNotification { Guild = Substitute.For<IGuild>() };

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await _client.DidNotReceive().GetGuildsAsync(options: Arg.Any<RequestOptions>());

        await notification
            .Guild
            .Received(1)
            .BulkOverwriteApplicationCommandsAsync(
                Arg.Is<ApplicationCommandProperties[]>(
                    x => x.Count(y => y is MessageCommandProperties) == ExpectedMessageCommandCount
                         && x.Count(y => y is SlashCommandProperties) == ExpectedSlashCommandCount),
                Arg.Any<RequestOptions>());
    }

    [Test]
    public async Task Handle_ReadyNotification_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var guild = Substitute.For<IGuild>();
        _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns([guild]);

        var notification = new ReadyNotification();

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await _client.Received(1).GetGuildsAsync(options: Arg.Any<RequestOptions>());

        await guild
            .Received(1)
            .BulkOverwriteApplicationCommandsAsync(
                Arg.Is<ApplicationCommandProperties[]>(
                    x => x.Count(y => y is MessageCommandProperties) == ExpectedMessageCommandCount
                         && x.Count(y => y is SlashCommandProperties) == ExpectedSlashCommandCount),
                Arg.Any<RequestOptions>());
    }

    [Test]
    public async Task Handle_ReadyNotification_NoGuilds_Returns(CancellationToken cancellationToken)
    {
        // Arrange
        _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns([]);

        var notification = new ReadyNotification();

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        _ = _translationProvider.DidNotReceive().TranslateCommandLangCodes;
        _ = _translationProvider.DidNotReceive().SupportedLanguages;
    }
}
