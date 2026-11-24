using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Telemetry;
using Microsoft.Extensions.Hosting;
using NeoSmart.Unicode;
using System.Diagnostics.Metrics;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProviderBaseTests : IDisposable
{
    private readonly Instrumentation _instrumentation;
    private readonly TranslationProviderFake _sut;

    public TranslationProviderBaseTests()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.ApplicationName.Returns("Test");
        _instrumentation = new Instrumentation(environment);

        _sut = new TranslationProviderFake(_instrumentation);
    }

    public void Dispose()
    {
        _instrumentation.Dispose();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TranslateAsync_RecordsTranslationCharactersOnlyOnSuccess(bool success)
    {
        // Arrange
        var measurements = new List<(long Value, object? Provider)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            // Only listen to this test's instance so parallel tests don't add measurements.
            if (ReferenceEquals(instrument, _instrumentation.TranslationCharacters))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            measurements.Add((value, tags.ToArray().Single(t => t.Key == "provider").Value)));

        meterListener.Start();

        // Act
        _sut.Outcome = success
            ? new TranslationOutcome(new TranslationResult { TargetLanguageCode = "fr", TranslatedText = "bonjour" })
            : new TranslationOutcome(new ProviderFailure("No translation returned."));
        var outcome = await _sut.TranslateAsync(
            new SupportedLanguage("fr", "French"), "hello", TestContext.Current.CancellationToken);

        // Assert
        outcome.Should().Be(_sut.Outcome);
        if (success)
        {
            measurements.Should().ContainSingle().Which.Should().Be((5L, nameof(TranslationProviderFake)));
        }
        else
        {
            measurements.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task TranslateAsync_CoreThrows_ReturnsProviderFailureWithCause()
    {
        // Arrange
        var error = new InvalidOperationException("Unexpected.");
        _sut.Error = error;

        // Act
        var outcome = await _sut.TranslateAsync(
            new SupportedLanguage("fr", "French"),
            "hello",
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Value.Should().BeOfType<ProviderFailure>().Which.Cause.Should().BeSameAs(error);
    }

    [Fact]
    public async Task TranslateByCountryAsync_LanguageNotFound_ReturnsUnsupportedTargetLanguage()
    {
        // Arrange
        var country = new Country(Emoji.FlagUnitedStates, ["unsupported-lang-code"]);

        const string text = "test";

        // Act
        var outcome = await _sut.TranslateByCountryAsync(country, text, TestContext.Current.CancellationToken);

        // Assert
        outcome.Value.Should().Be(new UnsupportedTargetLanguage(nameof(TranslationProviderFake)));
    }

    private sealed class TranslationProviderFake(Instrumentation instrumentation)
        : TranslationProviderBase(instrumentation, new LoggerFake<TranslationProviderFake>())
    {
        public TranslationOutcome Outcome { get; set; }

        public Exception? Error { get; set; }

        public override Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task<TranslationOutcome> TranslateCoreAsync(
            SupportedLanguage targetLanguage,
            string text,
            CancellationToken cancellationToken,
            string? sourceLangCode = null)
        {
            return Error is null ? Task.FromResult(Outcome) : Task.FromException<TranslationOutcome>(Error);
        }
    }
}
