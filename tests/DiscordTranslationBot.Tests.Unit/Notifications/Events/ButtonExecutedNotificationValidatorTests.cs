using Discord;
using DiscordTranslationBot.Notifications.Events;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class ButtonExecutedNotificationValidatorTests
{
    private readonly ButtonExecutedNotificationValidator _sut = new();

    [Test]
    public async Task Valid_Validates_WithNoErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new ButtonExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };

        // Act
        var results = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        results.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_Interaction_Validates_WithErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new ButtonExecutedNotification { Interaction = null! };

        // Act
        var results = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        results.ShouldHaveValidationErrorFor(x => x.Interaction);
    }
}
