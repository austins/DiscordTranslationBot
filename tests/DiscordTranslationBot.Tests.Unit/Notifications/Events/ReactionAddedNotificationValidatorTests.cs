using Discord;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Notifications.Events;
using FluentValidation.TestHelper;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class ReactionAddedNotificationValidatorTests
{
    private readonly ReactionAddedNotificationValidator _sut = new();

    [Test]
    public async Task Valid_Validates_WithNoErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = Substitute.For<IUserMessage>(),
            Channel = Substitute.For<IMessageChannel>(),
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_Validates_WithErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = null!,
            Channel = null!,
            ReactionInfo = null!
        };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Message);
        result.ShouldHaveValidationErrorFor(x => x.Channel);
        result.ShouldHaveValidationErrorFor(x => x.ReactionInfo);
    }
}
