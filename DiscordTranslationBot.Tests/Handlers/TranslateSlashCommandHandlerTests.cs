using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Notifications;
using DiscordTranslationBot.Providers.Translation;

namespace DiscordTranslationBot.Tests.Handlers;

[TestClass]
public sealed class TranslateSlashCommandHandlerTests
{
    private const string ProviderName = "Test Provider";
    private readonly TranslateSlashCommandHandler _sut;
    private readonly TranslationProviderBase _translationProvider;

    public TranslateSlashCommandHandlerTests()
    {
        _translationProvider = Substitute.For<TranslationProviderBase>();
        _translationProvider.ProviderName.Returns(ProviderName);

        _sut = new TranslateSlashCommandHandler(
            new[] { _translationProvider },
            Substitute.For<ILogger<TranslateSlashCommandHandler>>());
    }

    [TestMethod]
    public async Task Handle_SlashCommandExecutedNotification_Success()
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

        var command = Substitute.For<ISlashCommandInteraction>();
        command.Data.Returns(data);

        var user = Substitute.For<IUser>();
        user.Id.Returns(1UL);
        command.User.Returns(user);

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

        var notification = new SlashCommandExecutedNotification { Command = command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider.Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode));

        await command.Received(1).DeferAsync(false, Arg.Any<RequestOptions>());

        await command.Received(1)
            .FollowupAsync(
                Arg.Is<string>(text => text.Contains($"translated text using {ProviderName} from")),
                options: Arg.Any<RequestOptions>());
    }

    [TestMethod]
    public async Task Handle_SlashCommandExecutedNotification_NotTranslateCommand_Returns()
    {
        // Arrange
        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns("not_the_translate_command");

        var command = Substitute.For<ISlashCommandInteraction>();
        command.Data.Returns(data);

        var notification = new SlashCommandExecutedNotification { Command = command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _ = notification.Command.Data.Received(1).Name;
        _ = notification.Command.Data.DidNotReceive().Options;

        await notification.Command.DidNotReceive()
            .FollowupAsync(Arg.Any<string>(), ephemeral: Arg.Any<bool>(), options: Arg.Any<RequestOptions>());
    }

    [TestMethod]
    public async Task Handle_SlashCommandExecutedNotification_Returns_SourceTextIsEmpty()
    {
        // Arrange
        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns(SlashCommandConstants.TranslateCommandName);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(SlashCommandConstants.TranslateCommandTextOptionName);
        textOption.Value.Returns(string.Empty);

        data.Options.Returns(new List<IApplicationCommandInteractionDataOption> { textOption });

        var command = Substitute.For<ISlashCommandInteraction>();
        command.Data.Returns(data);

        var notification = new SlashCommandExecutedNotification { Command = command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await command.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await command.Received(1)
            .RespondAsync("No text to translate.", ephemeral: true, options: Arg.Any<RequestOptions>());

        await _translationProvider.DidNotReceive()
            .TranslateAsync(
                Arg.Any<SupportedLanguage>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<SupportedLanguage>());
    }

    [TestMethod]
    public async Task Handle_SlashCommandExecutedNotification_Returns_OnFailureToDetectSourceLanguage()
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

        var command = Substitute.For<ISlashCommandInteraction>();
        command.Data.Returns(data);

        var user = Substitute.For<IUser>();
        user.Id.Returns(1UL);
        command.User.Returns(user);

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

        var notification = new SlashCommandExecutedNotification { Command = command };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _translationProvider.Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                Arg.Any<CancellationToken>(),
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode));

        await command.Received(1)
            .FollowupAsync(
                "Couldn't detect the source language to translate from or the result is the same.",
                options: Arg.Any<RequestOptions>());
    }
}
