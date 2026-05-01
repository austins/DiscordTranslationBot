using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DiscordTranslationBot.Discord;

internal sealed class DiscordClientHealthCheck : IHealthCheck
{
    public const string HealthCheckName = "DiscordClient";

    private readonly DiscordSocketClient _client;

    public DiscordClientHealthCheck(IDiscordClient client)
    {
        _client = (DiscordSocketClient)client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var description = _client.ConnectionState.ToString();

        if (_client.ConnectionState is ConnectionState.Connected)
        {
            return HealthCheckResult.Healthy(
                description,
                new Dictionary<string, object> { { "GuildCount", _client.Guilds.Count } });
        }

        return _client.ConnectionState is ConnectionState.Connecting or ConnectionState.Disconnecting
            ? HealthCheckResult.Degraded(description)
            : HealthCheckResult.Unhealthy(description);
    }
}
