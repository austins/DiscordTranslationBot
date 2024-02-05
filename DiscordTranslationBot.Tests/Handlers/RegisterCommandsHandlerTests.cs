using Discord;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class RegisterCommandsHandlerTests
{
    private const string ProviderName = "Test Provider";
#pragma warning disable NUnit1032
    private readonly IDiscordClient _client;
#pragma warning restore NUnit1032
    private readonly RegisterCommandsHandler _sut;
    private readonly TranslationProviderBase _translationProvider;

    public RegisterCommandsHandlerTests()
    {
        _client = Substitute.For<IDiscordClient>();

        _translationProvider = Substitute.For<TranslationProviderBase>();
        _translationProvider.ProviderName.Returns(ProviderName);

        _sut = new RegisterCommandsHandler(
            _client,
            new[] { _translationProvider },
            new LoggerFake<RegisterCommandsHandler>());
    }

    [Test]
    public async Task Handle_ReadyNotification_Success()
    {
        // Arrange
        IReadOnlyCollection<IGuild> guilds = new List<IGuild>
        {
            Substitute.For<IGuild>(),
            Substitute.For<IGuild>()
        };

        _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns(guilds);

        _translationProvider.TranslateCommandLangCodes.Returns(new HashSet<string>());

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage>
            {
                new()
                {
                    LangCode = "en",
                    Name = "English"
                }
            });

        var notification = new ReadyNotification();

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        foreach (var guild in guilds)
        {
            await guild.Received(1)
                .CreateApplicationCommandAsync(Arg.Any<MessageCommandProperties>(), Arg.Any<RequestOptions>());

            await guild.Received(1)
                .CreateApplicationCommandAsync(Arg.Any<SlashCommandProperties>(), Arg.Any<RequestOptions>());
        }
    }

    [Test]
    public async Task Handle_ReadyNotification_NoGuilds_Returns()
    {
        // Arrange
        _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns(new List<IGuild>());

        var notification = new ReadyNotification();

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = _translationProvider.DidNotReceive().TranslateCommandLangCodes;
    }

    [Test]
    public async Task Handle_JoinedGuildNotification_Success()
    {
        // Arrange
        _translationProvider.TranslateCommandLangCodes.Returns(new HashSet<string>());

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage>
            {
                new()
                {
                    LangCode = "en",
                    Name = "English"
                }
            });

        var notification = new JoinedGuildNotification { Guild = Substitute.For<IGuild>() };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _client.DidNotReceive().GetGuildsAsync(options: Arg.Any<RequestOptions>());

        await notification.Guild.Received(1)
            .CreateApplicationCommandAsync(Arg.Any<MessageCommandProperties>(), Arg.Any<RequestOptions>());

        await notification.Guild.Received(1)
            .CreateApplicationCommandAsync(Arg.Any<SlashCommandProperties>(), Arg.Any<RequestOptions>());
    }
}
