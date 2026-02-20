using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProvidersOptionsTests
{
    [Fact]
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
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
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
        isValid.Should().BeFalse();

        validationResults.Count.Should().Be(4);

        validationResults
            .Should()
            .ContainSingle(x => x.ErrorMessage!.Contains(
                $"{nameof(AzureTranslatorOptions)}.{nameof(TranslationProviderOptionsBase.ApiUrl)}"));

        validationResults
            .Should()
            .ContainSingle(x =>
                x.ErrorMessage!.Contains($"{nameof(AzureTranslatorOptions)}.{nameof(AzureTranslatorOptions.Region)}"));

        validationResults
            .Should()
            .ContainSingle(x =>
                x.ErrorMessage!.Contains(
                    $"{nameof(AzureTranslatorOptions)}.{nameof(AzureTranslatorOptions.SecretKey)}"));

        validationResults
            .Should()
            .ContainSingle(x =>
                x.ErrorMessage!.Contains(
                    $"{nameof(LibreTranslateOptions)}.{nameof(TranslationProviderOptionsBase.ApiUrl)}"));
    }
}
