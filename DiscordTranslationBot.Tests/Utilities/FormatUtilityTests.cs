using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Tests.Utilities;

[TestClass]
public sealed class FormatUtilityTests
{
    [DataTestMethod]
    [DataRow(" textThatShouldBeTrimmed ", "textThatShouldBeTrimmed")]
    [DataRow("text with unicode 👻 emoji 🤔", "text with unicode  emoji")]
    [DataRow("<@000000000000000000> test", "test")]
    [DataRow("test <@!000000000000000000>", "test")]
    [DataRow("<:emote:123000000000000000>", "")]
    [DataRow("<:emote1:000000000000000000>", "")]
    [DataRow("<:1234:000000000000000123>", "")]
    [DataRow("test <a:test_emote:100000000000000123>", "test")]
    [DataRow("<a:1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A:100000000000000123>", "")]
    [DataRow("text with links http://example.com https://example.com test", "text with links   test")]
    [DataRow(
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
        result.Should().Be(expected);
    }
}
