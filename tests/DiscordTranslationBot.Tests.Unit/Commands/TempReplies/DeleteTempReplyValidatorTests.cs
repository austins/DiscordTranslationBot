using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class DeleteTempReplyValidatorTests
{
    private readonly DeleteTempReplyValidator _sut = new();

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Valid_ValidatesWithoutErrors(bool hasReactionInfo, CancellationToken cancellationToken)
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IUserMessage>(),
            SourceMessageId = 1UL,
            ReactionInfo = hasReactionInfo
                ? new ReactionInfo
                {
                    UserId = 1,
                    Emote = Substitute.For<IEmote>()
                }
                : null
        };

        // Act
        var result = await _sut.TestValidateAsync(command, cancellationToken: cancellationToken);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_Reply_HasValidationError(CancellationToken cancellationToken)
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = null!,
            SourceMessageId = 1UL,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(command, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reply);
    }
}
