using DiscordTranslationBot.Providers.Translation.Models;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation.Models;

public sealed class SupportedLanguageTests
{
    [Test]
    public void Equals_WhenOtherIsNull_ReturnsFalse()
    {
        // Arrange
        var language = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        // Act
#pragma warning disable CA1508
        var result = language.Equals(null);
#pragma warning restore CA1508

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void Equals_WhenSameInstance_ReturnsTrue()
    {
        // Arrange
        var language = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        // Act
        var result = language.Equals(language);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void Equals_WhenLangCodesMatch_ReturnsTrue()
    {
        // Arrange
        var language1 = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        var language2 = new SupportedLanguage
        {
            LangCode = "EN",
            Name = "English"
        };

        // Act
        var result = language1.Equals(language2);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void Equals_WhenLangCodesDontMatch_ReturnsFalse()
    {
        // Arrange
        var language1 = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        var language2 = new SupportedLanguage
        {
            LangCode = "es",
            Name = "Spanish"
        };

        // Act
        var result = language1.Equals(language2);

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void GetHashCode_WhenCalled_ReturnsHashCodeOfLangCode()
    {
        // Arrange
        var language = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        // Act
        var result = language.GetHashCode();

        // Assert
        result.ShouldBe("EN".GetHashCode(StringComparison.OrdinalIgnoreCase));
    }
}
