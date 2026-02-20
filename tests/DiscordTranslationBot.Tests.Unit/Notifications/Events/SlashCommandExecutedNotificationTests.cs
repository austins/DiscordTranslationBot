using Discord;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Notifications.Events;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class SlashCommandExecutedNotificationTests
{
    [Fact]
    public void Valid_Validates_WithNoErrors()
    {
        // Arrange
        var notification = new SlashCommandExecutedNotification
            { Interaction = Substitute.For<ISlashCommandInteraction>() };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_Interaction_Validates_WithErrors()
    {
        // Arrange
        var notification = new SlashCommandExecutedNotification { Interaction = null! };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults.Should().ContainSingle();
        validationResults[0].MemberNames.Should().ContainSingle().Which.Should().Be(nameof(notification.Interaction));
    }
}
