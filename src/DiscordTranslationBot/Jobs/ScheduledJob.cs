namespace DiscordTranslationBot.Jobs;

internal sealed class ScheduledJob
{
    public required Func<CancellationToken, ValueTask> Action { get; init; }

    public required DateTimeOffset ExecuteAt { get; init; }
}
