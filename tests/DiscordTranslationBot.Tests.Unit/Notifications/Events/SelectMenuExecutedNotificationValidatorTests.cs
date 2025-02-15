using Discord;
using DiscordTranslationBot.Notifications.Events;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class SelectMenuExecutedNotificationValidatorTests
{
    private readonly SelectMenuExecutedNotificationValidator _sut = new();

    [Test]
    public async Task Valid_Validates_WithNoErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new SelectMenuExecutedNotification { Interaction = Substitute.For<IComponentInteraction>() };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_Interaction_Validates_WithErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new SelectMenuExecutedNotification { Interaction = null! };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Interaction);
    }
}
