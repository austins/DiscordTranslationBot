using Discord;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using MediatR;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

public sealed class TranslateBySlashCommandHandlerTests
{
    private const string ProviderName = "Test Provider";
    private readonly IMediator _mediator;
    private readonly TranslateBySlashCommandHandler _sut;
    private readonly TranslationProviderBase _translationProvider;

    public TranslateBySlashCommandHandlerTests()
    {
        _translationProvider = Substitute.For<TranslationProviderBase>();
        _translationProvider.ProviderName.Returns(ProviderName);

        _mediator = Substitute.For<IMediator>();

        _sut = new TranslateBySlashCommandHandler(
            new[] { _translationProvider },
            _mediator,
            new LoggerFake<TranslateBySlashCommandHandler>());
    }

    [Test]
    public async Task Handle_TranslateBySlashCommand_Success()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage
        {
            LangCode = "fr",
            Name = "French"
        };

        var sourceLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        const string text = "text";

        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns(SlashCommandConstants.TranslateCommandName);

        var toOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        toOption.Name.Returns(SlashCommandConstants.TranslateCommandToOptionName);
        toOption.Value.Returns(targetLanguage.LangCode);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(SlashCommandConstants.TranslateCommandTextOptionName);
        textOption.Value.Returns(text);

        var fromOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        fromOption.Name.Returns(SlashCommandConstants.TranslateCommandFromOptionName);
        fromOption.Value.Returns(sourceLanguage.LangCode);

        data.Options.Returns(
            new List<IApplicationCommandInteractionDataOption>
            {
                toOption,
                textOption,
                fromOption
            });

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var user = Substitute.For<IUser>();
        user.Id.Returns(1UL);
        slashCommand.User.Returns(user);

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage>
            {
                sourceLanguage,
                targetLanguage
            });

        _translationProvider
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode))
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = null,
                    DetectedLanguageName = null,
                    TargetLanguageCode = targetLanguage.LangCode,
                    TargetLanguageName = targetLanguage.Name,
                    TranslatedText = "translated text"
                });

        var request = new TranslateBySlashCommand { SlashCommand = slashCommand };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _translationProvider.Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode));

        await slashCommand.Received(1).DeferAsync(false, Arg.Any<RequestOptions>());

        await slashCommand.Received(1)
            .FollowupAsync(
                Arg.Is<string>(text => text.Contains($"translated text using {ProviderName} from")),
                options: Arg.Any<RequestOptions>());
    }

    [Test]
    public async Task Handle_SlashCommandExecutedEvent_SendsCommand()
    {
        // Arrange
        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns(SlashCommandConstants.TranslateCommandName);

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var notification = new SlashCommandExecutedEvent { SlashCommand = slashCommand };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = notification.SlashCommand.Data.Received(1).Name;

        await _mediator.Received(1).Send(Arg.Any<TranslateBySlashCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_SlashCommandExecutedEvent_NotTranslateCommand_Returns()
    {
        // Arrange
        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns("not_the_translate_command");

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var notification = new SlashCommandExecutedEvent { SlashCommand = slashCommand };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = notification.SlashCommand.Data.Received(1).Name;

        await _mediator.DidNotReceive().Send(Arg.Any<TranslateBySlashCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_TranslateBySlashCommand_Returns_SourceTextIsEmpty()
    {
        // Arrange
        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns(SlashCommandConstants.TranslateCommandName);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(SlashCommandConstants.TranslateCommandTextOptionName);
        textOption.Value.Returns(string.Empty);

        data.Options.Returns(new List<IApplicationCommandInteractionDataOption> { textOption });

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var request = new TranslateBySlashCommand { SlashCommand = slashCommand };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await slashCommand.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await slashCommand.Received(1)
            .RespondAsync("No text to translate.", ephemeral: true, options: Arg.Any<RequestOptions>());

        await _translationProvider.DidNotReceive()
            .TranslateAsync(
                Arg.Any<SupportedLanguage>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<SupportedLanguage>());
    }

    [Test]
    public async Task Handle_TranslateBySlashCommand_Returns_OnFailureToDetectSourceLanguage()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage
        {
            LangCode = "fr",
            Name = "French"
        };
        var sourceLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        const string text = "text";

        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns(SlashCommandConstants.TranslateCommandName);

        var toOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        toOption.Name.Returns(SlashCommandConstants.TranslateCommandToOptionName);
        toOption.Value.Returns(targetLanguage.LangCode);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(SlashCommandConstants.TranslateCommandTextOptionName);
        textOption.Value.Returns(text);

        var fromOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        fromOption.Name.Returns(SlashCommandConstants.TranslateCommandFromOptionName);
        fromOption.Value.Returns(sourceLanguage.LangCode);

        data.Options.Returns(
            new List<IApplicationCommandInteractionDataOption>
            {
                toOption,
                textOption,
                fromOption
            });

        var slashCommand = Substitute.For<ISlashCommandInteraction>();
        slashCommand.Data.Returns(data);

        var user = Substitute.For<IUser>();
        user.Id.Returns(1UL);
        slashCommand.User.Returns(user);

        _translationProvider.SupportedLanguages.Returns(
            new HashSet<SupportedLanguage>
            {
                sourceLanguage,
                targetLanguage
            });

        _translationProvider
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode))
            .Returns(
                new TranslationResult
                {
                    DetectedLanguageCode = null,
                    DetectedLanguageName = null,
                    TargetLanguageCode = targetLanguage.LangCode,
                    TargetLanguageName = targetLanguage.Name,
                    TranslatedText = text
                });

        var request = new TranslateBySlashCommand { SlashCommand = slashCommand };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _translationProvider.Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode));

        await slashCommand.Received(1)
            .FollowupAsync(
                "Couldn't detect the source language to translate from or the result is the same.",
                options: Arg.Any<RequestOptions>());
    }
}
