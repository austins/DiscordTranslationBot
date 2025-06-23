using System.Threading.Channels;

namespace DiscordTranslationBot.Jobs;

internal sealed partial class Scheduler : IScheduler
{
    private readonly Channel<ScheduledJob> _channel;
    private readonly Log _log;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public Scheduler(ISender sender, TimeProvider timeProvider, ILogger<Scheduler> logger)
    {
        _sender = sender;
        _timeProvider = timeProvider;
        _log = new Log(logger);

        _channel = Channel.CreateUnboundedPrioritized(
            new UnboundedPrioritizedChannelOptions<ScheduledJob>
            {
                Comparer = Comparer<ScheduledJob>.Create((x, y) => x.ExecuteAt.CompareTo(y.ExecuteAt))
            });
    }

    public int Count => _channel.Reader.Count;

    public async Task ScheduleAsync(ICommand command, DateTimeOffset executeAt, CancellationToken cancellationToken)
    {
        if (executeAt <= _timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("Commands can only be scheduled to execute in the future.");
        }

        var job = new ScheduledJob
        {
            CommandName = command.GetType().Name,
            Action = async ct => await _sender.Send(command, ct),
            ExecuteAt = executeAt
        };

        await _channel.Writer.WriteAsync(job, cancellationToken);
        _log.ScheduledCommand(job.Id, job.CommandName, job.ExecuteAt.ToLocalTime(), Count);
    }

    public Task ScheduleAsync(ICommand command, TimeSpan executionDelay, CancellationToken cancellationToken)
    {
        return ScheduleAsync(command, _timeProvider.GetUtcNow() + executionDelay, cancellationToken);
    }

    public async Task<ScheduledJob?> GetNextJobDueAsync(CancellationToken cancellationToken)
    {
        if (_channel.Reader.TryPeek(out var job) && job.ExecuteAt <= _timeProvider.GetUtcNow())
        {
            job = await _channel.Reader.ReadAsync(cancellationToken);
            _log.DequeuedTask(job.Id, job.ExecuteAt.ToLocalTime(), Count);

            return job;
        }

        return null;
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Scheduled command '{commandName}' with ID {jobId} to be executed at {executeAt}. Total tasks in queue: {totalTasks}.")]
        public partial void ScheduledCommand(Guid jobId, string commandName, DateTimeOffset executeAt, int totalTasks);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Dequeued a task with ID {jobId} scheduled to be executed at {executeAt}. Remaining tasks in queue: {remainingTasks}.")]
        public partial void DequeuedTask(Guid jobId, DateTimeOffset executeAt, int remainingTasks);
    }
}

internal interface IScheduler
{
    /// <summary>
    /// The count tasks in the queue.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Queues a Mediator command to run at a specific time.
    /// </summary>
    /// <param name="command">Mediator command to schedule.</param>
    /// <param name="executeAt">Time to execute the job at.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ScheduleAsync(ICommand command, DateTimeOffset executeAt, CancellationToken cancellationToken);

    /// <summary>
    /// Queues a Mediator command to run at a specific time.
    /// </summary>
    /// <param name="command">Mediator command to schedule.</param>
    /// <param name="executionDelay">Delay for executing the job from now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ScheduleAsync(ICommand command, TimeSpan executionDelay, CancellationToken cancellationToken);

    /// <summary>
    /// Get the next scheduled job due to be executed.
    /// If a job exists, it is dequeued.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scheduled job to be executed or null.</returns>
    public Task<ScheduledJob?> GetNextJobDueAsync(CancellationToken cancellationToken);
}
