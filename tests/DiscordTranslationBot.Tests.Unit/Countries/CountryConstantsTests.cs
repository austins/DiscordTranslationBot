using DiscordTranslationBot.Countries;

namespace DiscordTranslationBot.Tests.Unit.Countries;

public sealed class CountryConstantsTests
{
    [Test]
    public void SupportedCountries_InitializesWithValidFlagEmojis_DoesNotThrow()
    {
        // Arrange
        var supportedCountries = () => CountryConstants.SupportedCountries;

        // Act & Assert
        supportedCountries.ShouldNotThrow();
        supportedCountries().ShouldNotBeEmpty();
    }
}
