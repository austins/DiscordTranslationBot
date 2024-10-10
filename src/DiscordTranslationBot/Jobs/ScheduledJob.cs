namespace DiscordTranslationBot.Jobs;

public sealed class ScheduledJob
{
    public required Func<CancellationToken, ValueTask> Action { get; init; }

    public required DateTimeOffset ExecuteAt { get; init; }
}
