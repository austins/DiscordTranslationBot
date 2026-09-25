using System.Threading.RateLimiting;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Limits how often each user can request translations to reduce excessive usage of the translation providers.
/// </summary>
internal sealed partial class TranslationRateLimiter : ITranslationRateLimiter, IDisposable
{
    public const int PermitLimit = 5;

    private static readonly TimeSpan Window = TimeSpan.FromSeconds(30);

    private readonly PartitionedRateLimiter<ulong> _limiter;
    private readonly Log _log;

    public TranslationRateLimiter(ILogger<TranslationRateLimiter> logger)
    {
        _log = new Log(logger);

        _limiter = PartitionedRateLimiter.Create<ulong, ulong>(userId =>
            RateLimitPartition.GetSlidingWindowLimiter(
                userId,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = PermitLimit,
                    Window = Window,
                    SegmentsPerWindow = 3,

                    // The partitioned limiter replenishes all partitions itself; avoids a timer per user.
                    AutoReplenishment = false
                }));
    }

    public bool TryAcquire(ulong userId)
    {
        using var lease = _limiter.AttemptAcquire(userId);
        if (!lease.IsAcquired)
        {
            _log.UserRateLimited(userId);
        }

        return lease.IsAcquired;
    }

    public void Dispose()
    {
        _limiter.Dispose();
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "User ID {userId} is rate limited for translations.")]
        public partial void UserRateLimited(ulong userId);
    }
}

internal interface ITranslationRateLimiter
{
    /// <summary>
    /// The message to respond with when a user is rate limited.
    /// </summary>
    public const string RateLimitedMessage = "You're translating too fast. Please wait a moment and try again.";

    /// <summary>
    /// Attempts to acquire a permit for a user to translate.
    /// </summary>
    /// <param name="userId">The ID of the user requesting a translation.</param>
    /// <returns><see langword="true" /> if the user can translate; <see langword="false" /> if rate limited.</returns>
    public bool TryAcquire(ulong userId);
}
