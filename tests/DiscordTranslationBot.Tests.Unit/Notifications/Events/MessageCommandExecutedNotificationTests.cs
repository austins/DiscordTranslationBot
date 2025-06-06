﻿using Discord;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Notifications.Events;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class MessageCommandExecutedNotificationTests
{
    [Test]
    public void Valid_Validates_WithNoErrors()
    {
        // Arrange
        var notification =
            new MessageCommandExecutedNotification { Interaction = Substitute.For<IMessageCommandInteraction>() };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Test]
    public void Invalid_Interaction_Validates_WithErrors()
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification { Interaction = null! };

        // Act
        var isValid = notification.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeFalse();

        var result = validationResults.ShouldHaveSingleItem();
        var memberName = result.MemberNames.ShouldHaveSingleItem();
        memberName.ShouldBe(nameof(notification.Interaction));
    }
}
