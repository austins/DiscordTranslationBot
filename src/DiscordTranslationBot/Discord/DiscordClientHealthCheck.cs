using Discord;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DiscordTranslationBot.Discord;

public sealed class DiscordClientHealthCheck : IHealthCheck
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
        var description = _client.ConnectionState.ToString();

        if (_client.ConnectionState is ConnectionState.Connected)
        {
            // The guilds list should already be cached by now.
            var guildCount =
                (await _client.GetGuildsAsync(options: new RequestOptions { CancelToken = cancellationToken })).Count;

            return HealthCheckResult.Healthy(
                description,
                new Dictionary<string, object> { { "GuildCount", guildCount } });
        }

        return _client.ConnectionState is ConnectionState.Connecting or ConnectionState.Disconnecting
            ? HealthCheckResult.Degraded(description)
            : HealthCheckResult.Unhealthy(description);
    }
}
