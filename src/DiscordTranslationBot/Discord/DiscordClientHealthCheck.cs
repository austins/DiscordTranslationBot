using Discord;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DiscordTranslationBot.Discord;

internal sealed class DiscordClientHealthCheck : IHealthCheck
{
    public const string HealthCheckName = "DiscordClient";

    private readonly IDiscordClient _client;

    public DiscordClientHealthCheck(IDiscordClient client)
    {
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            { nameof(IDiscordClient.ConnectionState), _client.ConnectionState }
        };

        if (_client.ConnectionState is ConnectionState.Connected)
        {
            // The guilds list should already be cached by now.
            data.Add(
                "GuildCount",
                (await _client.GetGuildsAsync(options: new RequestOptions { CancelToken = cancellationToken })).Count);

            return HealthCheckResult.Healthy(data: data);
        }

        return _client.ConnectionState is ConnectionState.Connecting or ConnectionState.Disconnecting
            ? HealthCheckResult.Degraded(data: data)
            : HealthCheckResult.Unhealthy(data: data);
    }
}
