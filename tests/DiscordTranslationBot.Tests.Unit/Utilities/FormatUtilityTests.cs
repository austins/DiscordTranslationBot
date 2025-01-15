using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Tests.Unit.Utilities;

public sealed class FormatUtilityTests
{
    [Theory]
    [InlineData(" textThatShouldBeTrimmed ", "textThatShouldBeTrimmed")]
    [InlineData("text with unicode 👻 emoji 🤔", "text with unicode  emoji")]
    [InlineData("<@000000000000000000> test", "test")]
    [InlineData("test <@!000000000000000000>", "test")]
    [InlineData("<:emote:123000000000000000>", "")]
    [InlineData("<:emote1:000000000000000000>", "")]
    [InlineData("<:1234:000000000000000123>", "")]
    [InlineData("test <a:test_emote:100000000000000123>", "test")]
    [InlineData("<a:1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A:100000000000000123>", "")]
    [InlineData("text with links http://example.com https://example.com/ test", "text with links   test")]
    [InlineData(
        """
        _markdown_ *markdown* `markdown` <a:1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A:100000000000000123>
        ```json
        { "test": "test" }
        ```

        - list item 1
        - list item 2

        1. numbered list item 1
        2. numbered list item 2

        ```
        <p>test</p>
        <p>test</p>
        ```

        ```test```
        [link](http://example.com)
        """,
        """
        markdown markdown markdown
        list item 1
        list item 2
        numbered list item 1
        numbered list item 2
        """)]
    public void SanitizeText_Returns_AsExpected(string text, string expected)
    {
        // Act
        var result = FormatUtility.SanitizeText(text);

        // Assert
        result.ShouldBe(expected);
    }
}
