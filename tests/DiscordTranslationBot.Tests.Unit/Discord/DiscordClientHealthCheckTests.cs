using Discord;
using DiscordTranslationBot.Discord;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DiscordTranslationBot.Tests.Unit.Discord;

public sealed class DiscordClientHealthCheckTests
{
    private readonly IDiscordClient _client;
    private readonly DiscordClientHealthCheck _sut;

    public DiscordClientHealthCheckTests()
    {
        _client = Substitute.For<IDiscordClient>();
        _sut = new DiscordClientHealthCheck(_client);
    }

    [Test]
    public async Task CheckHealthAsync_Returns_Healthy(CancellationToken cancellationToken)
    {
        // Arrange
        _client.ConnectionState.Returns(ConnectionState.Connected);

        var guilds = new List<IGuild> { Substitute.For<IGuild>() };
        _client.GetGuildsAsync().ReturnsForAnyArgs(guilds);

        var expectedDescription = _client.ConnectionState.ToString();
        var expectedData = new Dictionary<string, object> { { "GuildCount", guilds.Count } };

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe(expectedDescription);
        result.Data.ShouldBeEquivalentTo(expectedData);
    }

    [Test]
    [Arguments(ConnectionState.Connecting)]
    [Arguments(ConnectionState.Disconnecting)]
    public async Task CheckHealthAsync_Returns_Degraded(ConnectionState state, CancellationToken cancellationToken)
    {
        // Arrange
        _client.ConnectionState.Returns(state);

        var expectedDescription = _client.ConnectionState.ToString();

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldBe(expectedDescription);
        result.Data.ShouldBeEmpty();

        await _client.DidNotReceiveWithAnyArgs().GetGuildsAsync();
    }

    [Test]
    public async Task CheckHealthAsync_Returns_Unhealthy(CancellationToken cancellationToken)
    {
        // Arrange
        _client.ConnectionState.Returns(ConnectionState.Disconnected);

        var expectedDescription = _client.ConnectionState.ToString();

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe(expectedDescription);
        result.Data.ShouldBeEmpty();

        await _client.DidNotReceiveWithAnyArgs().GetGuildsAsync();
    }
}
