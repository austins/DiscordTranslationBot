using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;

namespace DiscordTranslationBot.Tests.Configuration.TranslationProviders;

public sealed class TranslationProvidersOptionsTests
{
    [Test]
    public void Valid_Options_ValidatesWithoutErrors()
    {
        // Arrange
        var options = new TranslationProvidersOptions
        {
            AzureTranslator = new AzureTranslatorOptions
            {
                Enabled = true,
                ApiUrl = new Uri("http://localhost", UriKind.Absolute),
                Region = "westus",
                SecretKey = "secret"
            },
            LibreTranslate = new LibreTranslateOptions { Enabled = false }
        };

        // Act
        var isValid = options.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Invalid_ProviderOptions_HasValidationErrors(string? stringValue)
    {
        // Arrange
        var options = new TranslationProvidersOptions
        {
            AzureTranslator = new AzureTranslatorOptions
            {
                Enabled = true,
                ApiUrl = null,
                Region = stringValue,
                SecretKey = stringValue
            },
            LibreTranslate = new LibreTranslateOptions
            {
                Enabled = true,
                ApiUrl = null
            }
        };

        // Act
        var isValid = options.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults.Should()
            .HaveCount(4)
            .And
            .ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(AzureTranslatorOptions)}.{nameof(TranslationProviderOptionsBase.ApiUrl)}"))
            .And.ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(LibreTranslateOptions)}.{nameof(TranslationProviderOptionsBase.ApiUrl)}"))
            .And.ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(AzureTranslatorOptions)}.{nameof(AzureTranslatorOptions.Region)}"))
            .And.ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(AzureTranslatorOptions)}.{nameof(AzureTranslatorOptions.SecretKey)}"));
    }
}
