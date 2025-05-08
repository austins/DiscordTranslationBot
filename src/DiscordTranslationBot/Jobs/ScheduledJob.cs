namespace DiscordTranslationBot.Jobs;

public sealed class ScheduledJob
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string CommandName { get; init; }

    public required Func<CancellationToken, ValueTask> Action { get; init; }

    public required DateTimeOffset ExecuteAt { get; init; }
}
