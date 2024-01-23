using Discord;
using DiscordTranslationBot.Commands.TempReply;
using DiscordTranslationBot.Models.Discord;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Commands.TempReply;

public sealed class SendTempReplyValidatorTests : TestBase
{
    private readonly SendTempReplyValidator _sut;

    public SendTempReplyValidatorTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        _sut = new SendTempReplyValidator();
    }

    [Fact]
    public async Task Valid_ValidatesWithoutErrors()
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            Reaction = new Reaction
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            },
            SourceMessage = Substitute.For<IMessage>(),
            DeletionDelayInSeconds = 20
        };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Invalid_Reply_HasValidationErrors(string? text)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = text!,
            Reaction = null,
            SourceMessage = null!
        };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Text);
        result.ShouldNotHaveValidationErrorFor(x => x.Reaction);
        result.ShouldHaveValidationErrorFor(x => x.SourceMessage);
    }
}
