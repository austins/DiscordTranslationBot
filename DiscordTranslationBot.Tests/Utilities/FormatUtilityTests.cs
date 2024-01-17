using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Tests.Utilities;

public sealed class FormatUtilityTests
{
    [Theory]
    [InlineData(" textThatShouldBeTrimmed ", "textThatShouldBeTrimmed")]
    [InlineData("<@000000000000000000> test", "test")]
    [InlineData("test <@!000000000000000000>", "test")]
    [InlineData("<:emote:123000000000000000>", "")]
    [InlineData("<:emote1:000000000000000000>", "")]
    [InlineData("<:1234:000000000000000123>", "")]
    [InlineData("test <a:test_emote:100000000000000123>", "test")]
    [InlineData("test <> testing", "test < testing")]
    [InlineData("<a:1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A:100000000000000123>", "")]
    [InlineData(
        """
_markdown_ *markdown* `markdown` <a:1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A:100000000000000123>
```json
{ "test": "test" }
```
""",
        """
markdown markdown markdown 
json
{ "test": "test" }
""")]
    public void SanitizeText_Returns_AsExpected(string text, string expected)
    {
        // Act
        var result = FormatUtility.SanitizeText(text);

        // Assert
        result.Should().Be(expected);
    }
}
