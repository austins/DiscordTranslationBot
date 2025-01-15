using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Tests.Unit.Utilities;

public sealed class FormatUtilityTests
{
    [Test]
    [Arguments(" textThatShouldBeTrimmed ", "textThatShouldBeTrimmed")]
    [Arguments("text with unicode 👻 emoji 🤔", "text with unicode  emoji")]
    [Arguments("<@000000000000000000> test", "test")]
    [Arguments("test <@!000000000000000000>", "test")]
    [Arguments("<:emote:123000000000000000>", "")]
    [Arguments("<:emote1:000000000000000000>", "")]
    [Arguments("<:1234:000000000000000123>", "")]
    [Arguments("test <a:test_emote:100000000000000123>", "test")]
    [Arguments("<a:1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A:100000000000000123>", "")]
    [Arguments("text with links http://example.com https://example.com/ test", "text with links   test")]
    [Arguments(
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
