using Discord;
using DiscordTranslationBot.Notifications.Events;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Notifications.Events;

public sealed class SlashCommandExecutedNotificationValidatorTests
{
    private readonly SlashCommandExecutedNotificationValidator _sut = new();

    [Test]
    public async Task Valid_Validates_WithNoErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification =
            new SlashCommandExecutedNotification { Interaction = Substitute.For<ISlashCommandInteraction>() };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_Interaction_Validates_WithErrors(CancellationToken cancellationToken)
    {
        // Arrange
        var notification = new SlashCommandExecutedNotification { Interaction = null! };

        // Act
        var result = await _sut.TestValidateAsync(notification, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Interaction);
    }
}
