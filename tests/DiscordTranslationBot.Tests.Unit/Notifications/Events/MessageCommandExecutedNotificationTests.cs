﻿using Discord;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Notifications.Events;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class MessageCommandExecutedNotificationTests
{
    [Fact]
    public void Valid_Validates_WithNoErrors()
    {
        // Arrange
        var notification =
            new MessageCommandExecutedNotification { Interaction = Substitute.For<IMessageCommandInteraction>() };

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
        var notification = new MessageCommandExecutedNotification { Interaction = null! };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(notification.Interaction)));
    }
}