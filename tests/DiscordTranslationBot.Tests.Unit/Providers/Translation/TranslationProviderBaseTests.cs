using DiscordTranslationBot.Countries.Exceptions;
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

    [Fact]
    public async Task TranslateAsync_Records_TranslationCharacters()
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
        await _sut.TranslateAsync(new SupportedLanguage("fr", "French"), "hello", TestContext.Current.CancellationToken);

        // Assert
        measurements.Should().ContainSingle().Which.Should().Be((5L, nameof(TranslationProviderFake)));
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_LanguageNotSupportedForCountryException_IfLangCodeNotFound()
    {
        // Arrange
        var country = new Country(Emoji.FlagUnitedStates, ["unsupported-lang-code"]);

        const string text = "test";

        // Act & Assert
        await _sut
            .Awaiting(x => x.TranslateByCountryAsync(country, text, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<LanguageNotSupportedForCountryException>();
    }

    private sealed class TranslationProviderFake(Instrumentation instrumentation)
        : TranslationProviderBase(instrumentation)
    {
        public override Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task<TranslationResult> TranslateCoreAsync(
            SupportedLanguage targetLanguage,
            string text,
            CancellationToken cancellationToken,
            string? sourceLangCode = null)
        {
            return Task.FromResult(new TranslationResult { TargetLanguageCode = "test", TranslatedText = text });
        }
    }
}
