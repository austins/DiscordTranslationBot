using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

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
        var isValid = options.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
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
        var isValid = options.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeFalse();

        validationResults.Count.ShouldBe(4);

        validationResults.ShouldContain(
            x => x.ErrorMessage!.Contains(
                $"{nameof(AzureTranslatorOptions)}.{nameof(TranslationProviderOptionsBase.ApiUrl)}"),
            1);

        validationResults.ShouldContain(
            x => x.ErrorMessage!.Contains($"{nameof(AzureTranslatorOptions)}.{nameof(AzureTranslatorOptions.Region)}"),
            1);

        validationResults.ShouldContain(
            x => x.ErrorMessage!.Contains(
                $"{nameof(AzureTranslatorOptions)}.{nameof(AzureTranslatorOptions.SecretKey)}"),
            1);

        validationResults.ShouldContain(
            x => x.ErrorMessage!.Contains(
                $"{nameof(LibreTranslateOptions)}.{nameof(TranslationProviderOptionsBase.ApiUrl)}"),
            1);
    }
}
