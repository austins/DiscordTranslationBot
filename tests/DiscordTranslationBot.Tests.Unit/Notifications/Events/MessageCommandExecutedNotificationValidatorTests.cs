using Discord;
using DiscordTranslationBot.Notifications.Events;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class MessageCommandExecutedNotificationValidatorTests
{
    private readonly MessageCommandExecutedNotificationValidator _sut = new();

    [Test]
    public async Task Valid_Validates_WithNoErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification =
            new MessageCommandExecutedNotification { Interaction = Substitute.For<IMessageCommandInteraction>() };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_Interaction_Validates_WithErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new MessageCommandExecutedNotification { Interaction = null! };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Interaction);
    }
}
