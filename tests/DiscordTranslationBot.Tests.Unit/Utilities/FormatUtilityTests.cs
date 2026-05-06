using DiscordTranslationBot.Utilities;

namespace DiscordTranslationBot.Tests.Unit.Utilities;

public sealed class FormatUtilityTests
{
    [Theory]
    [MemberData(nameof(TextData))]
    public void SanitizeText_Returns_AsExpected(string text, string expected)
    {
        // Act
        var result = FormatUtility.SanitizeText(text);

        // Assert
        result.Should().Be(expected);
    }

    public static TheoryData<string, string> TextData()
    {
        return new TheoryData<string, string>
        {
            { " textThatShouldBeTrimmed ", "textThatShouldBeTrimmed" },
            {
                $"text with unicode {NeoSmart.Unicode.Emoji.Ghost} emoji {NeoSmart.Unicode.Emoji.ThinkingFace}",
                "text with unicode  emoji"
            },
            { "<@000000000000000000> test", "test" },
            { "test <@!000000000000000000>", "test" },
            { "<:emote:123000000000000000>", string.Empty },
            { "<:emote1:000000000000000000>", string.Empty },
            { "<:1234:000000000000000123>", string.Empty },
            { "test <a:test_emote:100000000000000123>", "test" },
            { "<a:1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A1A:100000000000000123>", string.Empty },
            { "text with links http://example.com https://example.com/ test", "text with links   test" },
            {
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
                """
            }
        };
    }
}
