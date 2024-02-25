using Discord;
using DiscordTranslationBot.Discord;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DiscordTranslationBot.Tests.Unit.Discord;

public sealed class DiscordClientHealthCheckTests
{
#pragma warning disable NUnit1032
    private readonly IDiscordClient _client;
#pragma warning restore NUnit1032
    private readonly DiscordClientHealthCheck _sut;

    public DiscordClientHealthCheckTests()
    {
        _client = Substitute.For<IDiscordClient>();
        _sut = new DiscordClientHealthCheck(_client);
    }

    [Test]
    public async Task CheckHealthAsync_Returns_Healthy()
    {
        // Arrange
        _client.ConnectionState.Returns(ConnectionState.Connected);

        var guilds = new List<IGuild> { Substitute.For<IGuild>() };
        _client.GetGuildsAsync().ReturnsForAnyArgs(guilds);

        var expectedData = new Dictionary<string, object>
        {
            { nameof(IDiscordClient.ConnectionState), _client.ConnectionState },
            { "GuildCount", guilds.Count }
        };

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().BeEquivalentTo(expectedData);
    }

    [TestCase(ConnectionState.Connecting)]
    [TestCase(ConnectionState.Disconnecting)]
    public async Task CheckHealthAsync_Returns_Degraded(ConnectionState state)
    {
        // Arrange
        _client.ConnectionState.Returns(state);

        var expectedData = new Dictionary<string, object>
        {
            { nameof(IDiscordClient.ConnectionState), _client.ConnectionState }
        };

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Should().BeEquivalentTo(expectedData);

        await _client.DidNotReceiveWithAnyArgs().GetGuildsAsync();
    }

    [Test]
    public async Task CheckHealthAsync_Returns_Unhealthy()
    {
        // Arrange
        _client.ConnectionState.Returns(ConnectionState.Disconnected);

        var expectedData = new Dictionary<string, object>
        {
            { nameof(IDiscordClient.ConnectionState), _client.ConnectionState }
        };

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data.Should().BeEquivalentTo(expectedData);

        await _client.DidNotReceiveWithAnyArgs().GetGuildsAsync();
    }
}
