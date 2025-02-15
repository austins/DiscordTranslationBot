using Discord;
using DiscordTranslationBot.Notifications.Events;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class JoinedGuildNotificationValidatorTests
{
    private readonly JoinedGuildNotificationValidator _sut = new();

    [Test]
    public async Task Valid_Validates_WithNoErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new JoinedGuildNotification { Guild = Substitute.For<IGuild>() };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_Guild_Validates_WithErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new JoinedGuildNotification { Guild = null! };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Guild);
    }
}
