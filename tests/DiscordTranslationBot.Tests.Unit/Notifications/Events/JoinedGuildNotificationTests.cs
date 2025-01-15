using Discord;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Notifications.Events;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class JoinedGuildNotificationTests
{
    [Fact]
    public void Valid_Validates_WithNoErrors()
    {
        // Arrange
        var notification = new JoinedGuildNotification { Guild = Substitute.For<IGuild>() };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Fact]
    public void Invalid_Guild_Validates_WithErrors()
    {
        // Arrange
        var notification = new JoinedGuildNotification { Guild = null! };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeFalse();

        var result = validationResults.ShouldHaveSingleItem();
        var memberName = result.MemberNames.ShouldHaveSingleItem();
        memberName.ShouldBe(nameof(notification.Guild));
    }
}
