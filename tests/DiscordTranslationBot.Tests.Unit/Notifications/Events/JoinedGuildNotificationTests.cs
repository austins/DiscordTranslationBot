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
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_Guild_Validates_WithErrors()
    {
        // Arrange
        var notification = new JoinedGuildNotification { Guild = null! };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults.Should().ContainSingle();
        validationResults[0].MemberNames.Should().ContainSingle().Which.Should().Be(nameof(notification.Guild));
    }
}
