using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProvidersOptionsTests
{
    private readonly TranslationProvidersOptionsValidator _sut = new();

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
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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
                ApiUrl = new Uri("localhost", UriKind.Relative)
            }
        };

        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AzureTranslator.ApiUrl);
        result.ShouldHaveValidationErrorFor(x => x.AzureTranslator.Region);
        result.ShouldHaveValidationErrorFor(x => x.AzureTranslator.SecretKey);
        result.ShouldHaveValidationErrorFor(x => x.LibreTranslate.ApiUrl);
    }
}
