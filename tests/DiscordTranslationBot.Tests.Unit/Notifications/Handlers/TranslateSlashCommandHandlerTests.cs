﻿using Discord;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Notifications.Handlers;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Handlers;

public sealed class TranslateSlashCommandHandlerTests
{
    private readonly TranslateSlashCommandHandler _sut;
    private readonly ITranslationProvider _translationProvider;

    public TranslateSlashCommandHandlerTests()
    {
        _translationProvider = Substitute.For<ITranslationProvider>();

        var translationProviderFactory = Substitute.For<ITranslationProviderFactory>();
        translationProviderFactory.PrimaryProvider.Returns(_translationProvider);

        _sut = new TranslateSlashCommandHandler(
            translationProviderFactory,
            new LoggerFake<TranslateSlashCommandHandler>());
    }

    [Test]
    public async Task Handle_SlashCommandExecutedNotification_Success(CancellationToken cancellationToken)
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
        data.Name.Returns(SlashCommandConstants.Translate.CommandName);

        var toOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        toOption.Name.Returns(SlashCommandConstants.Translate.CommandToOptionName);
        toOption.Value.Returns(targetLanguage.LangCode);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(SlashCommandConstants.Translate.CommandTextOptionName);
        textOption.Value.Returns(text);

        var fromOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        fromOption.Name.Returns(SlashCommandConstants.Translate.CommandFromOptionName);
        fromOption.Value.Returns(sourceLanguage.LangCode);

        data.Options.Returns([toOption, textOption, fromOption]);

        var interaction = Substitute.For<ISlashCommandInteraction>();
        interaction.Data.Returns(data);

        var user = Substitute.For<IUser>();
        user.Id.Returns(1UL);
        interaction.User.Returns(user);

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
                cancellationToken,
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

        var notification = new SlashCommandExecutedNotification { Interaction = interaction };

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                cancellationToken,
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode));

        await interaction.Received(1).DeferAsync(false, Arg.Any<RequestOptions>());

        await interaction
            .Received(1)
            .FollowupAsync(
                Arg.Is<string>(textSent => textSent.Contains("translated text from")),
                options: Arg.Any<RequestOptions>());
    }

    [Test]
    public async Task Handle_SlashCommandExecutedEvent_NotTranslateCommand_Returns(CancellationToken cancellationToken)
    {
        // Arrange
        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns("not_the_translate_command");

        var interaction = Substitute.For<ISlashCommandInteraction>();
        interaction.Data.Returns(data);

        var notification = new SlashCommandExecutedNotification { Interaction = interaction };

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        _ = notification.Interaction.Data.Received(1).Name;
        _ = notification.Interaction.Data.DidNotReceive().Options;
    }

    [Test]
    public async Task Handle_SlashCommandExecutedEvent_Returns_SourceTextIsEmpty(CancellationToken cancellationToken)
    {
        // Arrange
        var data = Substitute.For<IApplicationCommandInteractionData>();
        data.Name.Returns(SlashCommandConstants.Translate.CommandName);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(SlashCommandConstants.Translate.CommandTextOptionName);
        textOption.Value.Returns(string.Empty);

        data.Options.Returns([textOption]);

        var interaction = Substitute.For<ISlashCommandInteraction>();
        interaction.Data.Returns(data);

        var notification = new SlashCommandExecutedNotification { Interaction = interaction };

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await interaction.DidNotReceive().DeferAsync(Arg.Any<bool>(), Arg.Any<RequestOptions>());

        await interaction
            .Received(1)
            .RespondAsync("No text to translate.", ephemeral: true, options: Arg.Any<RequestOptions>());

        await _translationProvider.DidNotReceiveWithAnyArgs().TranslateAsync(default!, default!, cancellationToken);
    }

    [Test]
    public async Task Handle_SlashCommandExecutedEvent_Returns_OnFailureToDetectSourceLanguage(
        CancellationToken cancellationToken)
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
        data.Name.Returns(SlashCommandConstants.Translate.CommandName);

        var toOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        toOption.Name.Returns(SlashCommandConstants.Translate.CommandToOptionName);
        toOption.Value.Returns(targetLanguage.LangCode);

        var textOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        textOption.Name.Returns(SlashCommandConstants.Translate.CommandTextOptionName);
        textOption.Value.Returns(text);

        var fromOption = Substitute.For<IApplicationCommandInteractionDataOption>();
        fromOption.Name.Returns(SlashCommandConstants.Translate.CommandFromOptionName);
        fromOption.Value.Returns(sourceLanguage.LangCode);

        data.Options.Returns([toOption, textOption, fromOption]);

        var interaction = Substitute.For<ISlashCommandInteraction>();
        interaction.Data.Returns(data);

        var user = Substitute.For<IUser>();
        user.Id.Returns(1UL);
        interaction.User.Returns(user);

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
                cancellationToken,
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

        var notification = new SlashCommandExecutedNotification { Interaction = interaction };

        // Act
        await _sut.Handle(notification, cancellationToken);

        // Assert
        await _translationProvider
            .Received(1)
            .TranslateAsync(
                Arg.Is<SupportedLanguage>(x => x.LangCode == targetLanguage.LangCode),
                text,
                cancellationToken,
                Arg.Is<SupportedLanguage>(x => x.LangCode == sourceLanguage.LangCode));

        await interaction
            .Received(1)
            .FollowupAsync(
                "Couldn't detect the source language to translate from or the result is the same.",
                options: Arg.Any<RequestOptions>());
    }
}
