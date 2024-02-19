using DiscordTranslationBot.Configuration.TranslationProviders;

namespace DiscordTranslationBot.Tests.Configuration.TranslationProviders;

public sealed class TranslationProvidersOptionsTests : ValidationTestBase
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
        var (results, isValid) = ValidateObject(options);

        // Assert
        results.Should().BeEmpty();
        isValid.Should().BeTrue();
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
        var (results, isValid) = ValidateObject(options);

        // Assert
        results.Should().HaveCount(4);

        results.Should()
            .ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(AzureTranslatorOptions)}.{nameof(TranslationProviderOptionsBase.ApiUrl)}"));

        results.Should()
            .ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(LibreTranslateOptions)}.{nameof(TranslationProviderOptionsBase.ApiUrl)}"));

        results.Should()
            .ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(AzureTranslatorOptions)}.{nameof(AzureTranslatorOptions.Region)}"));

        results.Should()
            .ContainSingle(
                x => x.ErrorMessage!.Contains(
                    $"{nameof(AzureTranslatorOptions)}.{nameof(AzureTranslatorOptions.SecretKey)}"));

        isValid.Should().BeFalse();
    }
}
