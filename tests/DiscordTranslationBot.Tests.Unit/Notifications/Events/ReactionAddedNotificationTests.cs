using Discord;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Notifications.Events;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class ReactionAddedNotificationTests
{
    [Fact]
    public void Valid_Validates_WithNoErrors()
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
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_Validates_WithErrors()
    {
        // Arrange
        var notification = new ReactionAddedNotification
        {
            Message = null!,
            Channel = null!,
            ReactionInfo = null!
        };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults.Count.Should().Be(3);
        validationResults.Should().ContainSingle(x => x.MemberNames.All(y => y == nameof(notification.Message)));
        validationResults.Should().ContainSingle(x => x.MemberNames.All(y => y == nameof(notification.Channel)));
        validationResults.Should().ContainSingle(x => x.MemberNames.All(y => y == nameof(notification.ReactionInfo)));
    }
}
