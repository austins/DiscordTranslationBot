using Discord;
using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class SlashCommandExecutedHandlerTests
{
    private const string ProviderName = "Test Provider";
    private readonly IDiscordClient _client;
    private readonly IMediator _mediator;
    private readonly SlashCommandExecutedHandler _sut;
    private readonly ITranslationProvider _translationProvider;

    public SlashCommandExecutedHandlerTests()
    {
        _mediator = Substitute.For<IMediator>();

        _translationProvider = Substitute.For<ITranslationProvider>();
        _translationProvider.ProviderName.Returns(ProviderName);

        _client = Substitute.For<IDiscordClient>();

        _sut = new SlashCommandExecutedHandler(
            _mediator,
            new[] { _translationProvider },
            _client,
            Substitute.For<ILogger<SlashCommandExecutedHandler>>()
        );
    }

    [Fact]
    public async Task Handle_SlashCommandExecutedNotification_Success()
    {
        // Arrange
        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns(CommandConstants.TranslateCommandName);

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var notification = new SlashCommandExecutedNotification { Command = slashCommand };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator
            .Received(1)
            .Send(Arg.Any<ProcessTranslateCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegisterSlashCommands_Returns_IfNoGuildsFound()
    {
        // Arrange
        _client.GetGuildsAsync().Returns(new List<IGuild>());

        var command = new RegisterSlashCommands();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _client.Received(1).GetGuildsAsync(Arg.Any<CacheMode>(), Arg.Any<RequestOptions>());
        _ = _translationProvider.DidNotReceive().TranslateCommandLangCodes;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_RegisterSlashCommands_Success(bool isSpecificGuild)
    {
        // Arrange
        IReadOnlyList<IGuild> guilds = isSpecificGuild
            ? new List<IGuild> { Substitute.For<IGuild>() }
            : new List<IGuild> { Substitute.For<IGuild>(), Substitute.For<IGuild>() };

        _translationProvider.TranslateCommandLangCodes.Returns(new HashSet<string>());

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage>
            {
                new() { LangCode = "en", Name = "English" }
            }
        );

        if (!isSpecificGuild)
        {
            _client.GetGuildsAsync(options: Arg.Any<RequestOptions>()).Returns(guilds);
        }

        var command = new RegisterSlashCommands { Guild = isSpecificGuild ? guilds[0] : null };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
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
    public async Task Handle_RegisterSlashCommands_WithTranslateCommandLangCodes_Success()
    {
        // Arrange
        var guild = Substitute.For<IGuild>();

        _translationProvider.TranslateCommandLangCodes.Returns(new HashSet<string> { "en" });

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage>
            {
                new() { LangCode = "en", Name = "English" }
            }
        );

        var command = new RegisterSlashCommands { Guild = guild };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _ = _translationProvider.Received(2).TranslateCommandLangCodes;

        await guild
            .Received(1)
            .CreateApplicationCommandAsync(
                Arg.Any<ApplicationCommandProperties>(),
                Arg.Any<RequestOptions>()
            );
    }

    [Fact]
    public async Task Handle_ProcessTranslateCommand_Success()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage { LangCode = "fr", Name = "French" };
        var sourceLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        const string text = "text";

        var data = Substitute.For<IApplicationCommandInteractionData>();

        var toOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        toOption.Name.Returns(CommandConstants.TranslateCommandToOptionName);
        toOption.Value.Returns(targetLanguage.LangCode);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(CommandConstants.TranslateCommandTextOptionName);
        textOption.Value.Returns(text);

        var fromOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        fromOption.Name.Returns(CommandConstants.TranslateCommandFromOptionName);
        fromOption.Value.Returns(sourceLanguage.LangCode);

        data.Options.Returns(
            new List<IApplicationCommandInteractionDataOption> { toOption, textOption, fromOption }
        );

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var user = Substitute.For<IUser>();
        user.Id.Returns(1UL);
        slashCommand.User.Returns(user);

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage> { sourceLanguage, targetLanguage }
        );

        _translationProvider
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode)
            )
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = null,
                    DetectedLanguageName = null,
                    TargetLanguageCode = targetLanguage.LangCode,
                    TargetLanguageName = targetLanguage.Name,
                    TranslatedText = "translated text"
                }
            );

        var command = new ProcessTranslateCommand { Command = slashCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode)
            );

        await slashCommand
            .Received(1)
            .RespondAsync(
                Arg.Is<string>(text => text.Contains($"translated text using {ProviderName} from"))
            );
    }

    [Fact]
    public async Task Handle_ProcessTranslateCommand_Returns_SourceTextIsEmpty()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage { LangCode = "fr", Name = "French" };

        var data = Substitute.For<IApplicationCommandInteractionData>();

        var toOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        toOption.Name.Returns(CommandConstants.TranslateCommandToOptionName);
        toOption.Value.Returns(targetLanguage.LangCode);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(CommandConstants.TranslateCommandTextOptionName);
        textOption.Value.Returns(string.Empty);

        data.Options.Returns(
            new List<IApplicationCommandInteractionDataOption> { toOption, textOption }
        );

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var command = new ProcessTranslateCommand { Command = slashCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await slashCommand.Received(1).RespondAsync("Nothing to translate.", ephemeral: true);

        await _translationProvider
            .DidNotReceive()
            .TranslateAsync(
                Arg.Any<SupportedLanguage>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<SupportedLanguage>()
            );
    }

    [Fact]
    public async Task Handle_ProcessTranslateCommand_Returns_OnFailuretoDetectSourceLanguage()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage { LangCode = "fr", Name = "French" };
        var sourceLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        const string text = "text";

        var data = Substitute.For<IApplicationCommandInteractionData>();

        var toOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        toOption.Name.Returns(CommandConstants.TranslateCommandToOptionName);
        toOption.Value.Returns(targetLanguage.LangCode);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(CommandConstants.TranslateCommandTextOptionName);
        textOption.Value.Returns(text);

        var fromOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        fromOption.Name.Returns(CommandConstants.TranslateCommandFromOptionName);
        fromOption.Value.Returns(sourceLanguage.LangCode);

        data.Options.Returns(
            new List<IApplicationCommandInteractionDataOption> { toOption, textOption, fromOption }
        );

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var user = Substitute.For<IUser>();
        user.Id.Returns(1UL);
        slashCommand.User.Returns(user);

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage> { sourceLanguage, targetLanguage }
        );

        _translationProvider
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode)
            )
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = null,
                    DetectedLanguageName = null,
                    TargetLanguageCode = targetLanguage.LangCode,
                    TargetLanguageName = targetLanguage.Name,
                    TranslatedText = text
                }
            );

        var command = new ProcessTranslateCommand { Command = slashCommand };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode)
            );

        await slashCommand
            .Received(1)
            .RespondAsync(
                "Couldn't detect the source language to translate from or the result is the same.",
                ephemeral: true
            );
    }
}
