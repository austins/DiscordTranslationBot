using Discord;
using DiscordTranslationBot.Commands.DiscordCommands;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using Mediator;

namespace DiscordTranslationBot.Tests.Unit.Commands.DiscordCommands;

public sealed class RegisterDiscordCommandsHandlerTests
{
    private const string ProviderName = "Test Provider";
    private readonly IDiscordClient _client;
    private readonly IMediator _mediator;
    private readonly RegisterDiscordCommandsHandler _sut;
    private readonly TranslationProviderBase _translationProvider;

    public RegisterDiscordCommandsHandlerTests()
    {
        _client = Substitute.For<IDiscordClient>();

        _translationProvider = Substitute.For<TranslationProviderBase>();
        _translationProvider.ProviderName.Returns(ProviderName);

        _mediator = Substitute.For<IMediator>();

        _sut = new RegisterDiscordCommandsHandler(
            _client,
            new[] { _translationProvider },
            _mediator,
            new LoggerFake<RegisterDiscordCommandsHandler>());
    }

    [Fact]
    public async Task Handle_JoinedGuildEvent_Success()
    {
        // Arrange
        var notification = new JoinedGuildEvent { Guild = Substitute.For<IGuild>() };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<RegisterDiscordCommands>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReadyEvent_Success()
    {
        // Arrange
        _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns([Substitute.For<IGuild>()]);

        var notification = new ReadyEvent();

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<RegisterDiscordCommands>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReadyEvent_NoGuilds_Returns()
    {
        // Arrange
        _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns([]);

        var notification = new ReadyEvent();

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<RegisterDiscordCommands>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegisterDiscordCommands_Success()
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

        var guild = Substitute.For<IGuild>();
        var command = new RegisterDiscordCommands { Guilds = [guild] };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _client.DidNotReceive().GetGuildsAsync(options: Arg.Any<RequestOptions>());

        await guild
            .Received(1)
            .CreateApplicationCommandAsync(Arg.Any<MessageCommandProperties>(), Arg.Any<RequestOptions>());

        await guild
            .Received(1)
            .CreateApplicationCommandAsync(Arg.Any<SlashCommandProperties>(), Arg.Any<RequestOptions>());
    }
}
