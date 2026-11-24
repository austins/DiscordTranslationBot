using Discord;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProviderFactoryTests
{
    private readonly ITranslationProvider _lastProvider;
    private readonly ITranslationProvider _primaryProvider;
    private readonly TranslationProviderFactory _sut;

    public TranslationProviderFactoryTests()
    {
        _primaryProvider = Substitute.For<ITranslationProvider>();
        _lastProvider = Substitute.For<ITranslationProvider>();

        _sut = new TranslationProviderFactory(
            [_primaryProvider, _lastProvider],
            new LoggerFake<TranslationProviderFactory>());
    }

    [Fact]
    public async Task Providers_ThrowsIfNotInitialized()
    {
        // Act + Assert
        _sut.Invoking(x => x.PrimaryProvider).Should().Throw<InvalidOperationException>();
        _sut.Invoking(x => x.GetSupportedLanguagesForOptions()).Should().Throw<InvalidOperationException>();

        await _sut
            .Awaiting(x => x.TranslateAsync(
                (_, _) => Task.FromResult<TranslationOutcome>(new ProviderFailure("Not available.")),
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Providers_ReturnsIfInitialized()
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        // Act + Assert
        _sut.PrimaryProvider.Should().Be(_primaryProvider);
    }

    [Fact]
    public async Task InitializeProvidersAsync_Success()
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        _lastProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        // Act
        var result = await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        await _primaryProvider.Received(1).InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken);
        await _lastProvider.Received(1).InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InitializeProvidersAsync_NoProviders_ReturnsFalse()
    {
        // Arrange
        var logger = new LoggerFake<TranslationProviderFactory>();
        var sut = new TranslationProviderFactory([], logger);

        // Act
        var result = await sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].LogLevel.Should().Be(LogLevel.Error);
        logger
            .Entries[0]
            .Message
            .Should()
            .Be("No translation providers enabled. Please configure and enable at least one translation provider.");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetSupportedLanguagesForOptions_Success(bool hasTranslateCommandLangCodes, bool fillRemaining)
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        _lastProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        var supportedLanguages = new Dictionary<string, string>
        {
            { "c", "Charlie" },
            { "d", "Delta" },
            { "a", "Alpha" },
            { "b", "Beta" }
        }.ToFrozenDictionary();

        FrozenSet<string> translateCommandLangCodes;
        ImmutableArray<SupportedLanguage> expectedResult;

        if (!hasTranslateCommandLangCodes)
        {
            // Empty TranslateCommandLangCodes - take first N from SupportedLanguages.
            _primaryProvider.TranslateCommandLangCodes.Returns([]);

            expectedResult =
            [
                .. supportedLanguages
                    .Take(SlashCommandOptionBuilder.MaxChoiceCount)
                    .Select(lc => new SupportedLanguage(lc.Key, lc.Value))
                    .OrderBy(l => l.Name, StringComparer.Ordinal)
            ];
        }
        else if (fillRemaining)
        {
            // TranslateCommandLangCodes has fewer codes than MaxOptionsCount - fill from remaining.
            translateCommandLangCodes = new HashSet<string> { "a" }.ToFrozenSet();
            _primaryProvider.TranslateCommandLangCodes.Returns(translateCommandLangCodes);

            expectedResult =
            [
                new SupportedLanguage("a", "Alpha"),
                new SupportedLanguage("b", "Beta"),
                new SupportedLanguage("c", "Charlie"),
                new SupportedLanguage("d", "Delta")
            ];
        }
        else
        {
            // TranslateCommandLangCodes has fewer codes than MaxOptionsCount - fill from remaining.
            translateCommandLangCodes = new HashSet<string>
            {
                "a",
                "b"
            }.ToFrozenSet();

            _primaryProvider.TranslateCommandLangCodes.Returns(translateCommandLangCodes);

            expectedResult =
            [
                new SupportedLanguage("a", "Alpha"),
                new SupportedLanguage("b", "Beta"),
                new SupportedLanguage("c", "Charlie"),
                new SupportedLanguage("d", "Delta")
            ];
        }

        _primaryProvider.SupportedLanguages.Returns(supportedLanguages);

        // Act
        var result = _sut.GetSupportedLanguagesForOptions();

        // Assert
        result.Should().BeEquivalentTo(expectedResult, o => o.WithStrictOrdering());
        _ = _primaryProvider.Received().TranslateCommandLangCodes;
        _ = _primaryProvider.Received().SupportedLanguages;
        _ = _lastProvider.DidNotReceive().TranslateCommandLangCodes;
        _ = _lastProvider.DidNotReceive().SupportedLanguages;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TranslateAsync_ReturnsFirstSuccess(bool primarySucceeds)
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        _lastProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        var translationResult = new TranslationResult { TargetLanguageCode = "a" };
        var expectedOutcome = new TranslationOutcome(translationResult);
        _primaryProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(primarySucceeds ? expectedOutcome : new TranslationOutcome(new ProviderFailure("Failed.")));
        _lastProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(expectedOutcome);

        // Act
        var result = await _sut.TranslateAsync(
            async (translationProvider, ct) =>
                await translationProvider.TranslateAsync(new SupportedLanguage("a", "a"), "text", ct),
            TestContext.Current.CancellationToken);

        // Assert
        result.Value.Should().BeSameAs(translationResult);

        await _primaryProvider
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);

        await _lastProvider
            .ReceivedWithAnyArgs(primarySucceeds ? 0 : 1)
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TranslateAsync_NoSuccess_ReturnsLastOutcome(bool lastProviderSupportsTarget)
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        _lastProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        TranslationOutcome failure = new ProviderFailure("Failed.");
        TranslationOutcome unsupported = new UnsupportedTargetLanguage("provider");
        var expected = lastProviderSupportsTarget ? failure : unsupported;

        _primaryProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(lastProviderSupportsTarget ? unsupported : failure);

        _lastProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(expected);

        // Act
        var outcome = await _sut.TranslateAsync(
            (provider, ct) => provider.TranslateAsync(new SupportedLanguage("a", "a"), "text", ct),
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Should().Be(expected);
    }

    [Fact]
    public async Task TranslateAsync_ActionThrows_FailsOverToNextProvider()
    {
        // Arrange
        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        var expected = new TranslationOutcome(new TranslationResult { TargetLanguageCode = "a" });
        _primaryProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ThrowsAsyncForAnyArgs<InvalidOperationException>();
        _lastProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(expected);

        // Act
        var outcome = await _sut.TranslateAsync(
            (provider, ct) => provider.TranslateAsync(new SupportedLanguage("a", "a"), "text", ct),
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Should().Be(expected);
    }

    [Fact]
    public async Task TranslateAsync_Cancelled_DoesNotFailOver()
    {
        // Arrange
        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        _primaryProvider
            .TranslateAsync(default!, default!, cancellationToken)
            .ReturnsForAnyArgs(_ =>
            {
                cts.Cancel();
                return new TranslationOutcome(new ProviderFailure("Request cancelled."));
            });

        // Act & Assert
        await _sut
            .Awaiting(x => x.TranslateAsync(
                async (translationProvider, ct) =>
                    await translationProvider.TranslateAsync(new SupportedLanguage("a", "a"), "text", ct),
                cancellationToken))
            .Should()
            .ThrowAsync<OperationCanceledException>();

        await _lastProvider.DidNotReceiveWithAnyArgs().TranslateAsync(default!, default!, cancellationToken);
    }
}
