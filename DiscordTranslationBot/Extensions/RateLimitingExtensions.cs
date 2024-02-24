using Microsoft.AspNetCore.RateLimiting;

namespace DiscordTranslationBot.Extensions;

internal static class RateLimitingExtensions
{
    public const string HealthCheckRateLimiterPolicyName = "HealthCheckRateLimiter";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(
            o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                o.AddTokenBucketLimiter(
                    HealthCheckRateLimiterPolicyName,
                    l =>
                    {
                        l.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                        l.TokensPerPeriod = 10;
                        l.TokenLimit = l.TokensPerPeriod * 2;
                        l.QueueLimit = l.TokensPerPeriod / 2;
                    });
            });
    }
}
