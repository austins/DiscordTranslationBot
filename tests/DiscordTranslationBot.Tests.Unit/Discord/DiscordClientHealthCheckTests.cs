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

    [Fact]
    public async Task CheckHealthAsync_Returns_Healthy()
    {
        // Arrange
        _client.ConnectionState.Returns(ConnectionState.Connected);

        var guilds = new List<IGuild> { Substitute.For<IGuild>() };
        _client.GetGuildsAsync().ReturnsForAnyArgs(guilds);

        var expectedDescription = _client.ConnectionState.ToString();
        var expectedData = new Dictionary<string, object> { { "GuildCount", guilds.Count } };

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be(expectedDescription);
        result.Data.Should().BeEquivalentTo(expectedData);
    }

    [Theory]
    [InlineData(ConnectionState.Connecting)]
    [InlineData(ConnectionState.Disconnecting)]
    public async Task CheckHealthAsync_Returns_Degraded(ConnectionState state)
    {
        // Arrange
        _client.ConnectionState.Returns(state);

        var expectedDescription = _client.ConnectionState.ToString();

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be(expectedDescription);
        result.Data.Should().BeEmpty();

        await _client.DidNotReceiveWithAnyArgs().GetGuildsAsync();
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy()
    {
        // Arrange
        _client.ConnectionState.Returns(ConnectionState.Disconnected);

        var expectedDescription = _client.ConnectionState.ToString();

        // Act
        var result = await _sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be(expectedDescription);
        result.Data.Should().BeEmpty();

        await _client.DidNotReceiveWithAnyArgs().GetGuildsAsync();
    }
}
