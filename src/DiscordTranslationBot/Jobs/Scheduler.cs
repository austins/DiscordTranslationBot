using System.Diagnostics.CodeAnalysis;

namespace DiscordTranslationBot.Jobs;

public sealed partial class Scheduler : IScheduler
{
    /// <summary>
    /// The threshold of the count of dequeued tasks since trim that will allow trimming once exceeded.
    /// </summary>
    /// <remarks>
    /// This is a simple solution to reduce unnecessary allocation for our use case.
    /// If this threshold is not effective, we may adjust it or consider another solution.
    /// </remarks>
    private const int DequeuedSinceTrimThreshold = 100;

    private readonly object _lock = new();
    private readonly Log _log;
    private readonly IMediator _mediator;

    /// <summary>
    /// The priority queue.
    /// </summary>
    /// <remarks>
    /// This is not thread-safe, so locking must be implemented as tasks can be enqueued across threads.
    /// </remarks>
    private readonly PriorityQueue<Func<CancellationToken, Task>, DateTimeOffset> _queue = new();

    private readonly TimeProvider _timeProvider;
    private int _dequeuedSinceTrim;

    public Scheduler(IMediator mediator, TimeProvider timeProvider, ILogger<Scheduler> logger)
    {
        _mediator = mediator;
        _timeProvider = timeProvider;
        _log = new Log(logger);
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    public void Schedule(ICommand command, DateTimeOffset executeAt)
    {
        if (executeAt <= _timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("Commands can only be scheduled to execute in the future.");
        }

        lock (_lock)
        {
            _queue.Enqueue(async ct => await _mediator.Send(command, ct), executeAt);
            _log.ScheduledCommand(command.GetType().Name, executeAt.ToLocalTime(), _queue.Count);
        }
    }

    public void Schedule(ICommand command, TimeSpan executionDelay)
    {
        Schedule(command, _timeProvider.GetUtcNow() + executionDelay);
    }

    public bool TryGetNextTask([NotNullWhen(true)] out Func<CancellationToken, Task>? task)
    {
        lock (_lock)
        {
            if (_queue.TryPeek(out _, out var executeAt) && executeAt <= _timeProvider.GetUtcNow())
            {
                task = _queue.Dequeue();
                _log.DequeuedTask(executeAt.ToLocalTime(), _queue.Count);

                // Optimize memory usage if number of dequeued before the next trim exceeds threshold.
                _dequeuedSinceTrim++;
                if (_dequeuedSinceTrim > DequeuedSinceTrimThreshold)
                {
                    _queue.TrimExcess();
                    _log.TrimmedExcess(DequeuedSinceTrimThreshold);
                    _dequeuedSinceTrim = 0;
                }

                return true;
            }
        }

        task = null;
        return false;
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Scheduled command '{commandName}' to be executed at {executeAt}. Total tasks in queue: {totalTasks}.")]
        public partial void ScheduledCommand(string commandName, DateTimeOffset executeAt, int totalTasks);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Dequeued a task scheduled to be executed at {executeAt}. Remaining tasks in queue: {remainingTasks}.")]
        public partial void DequeuedTask(DateTimeOffset executeAt, int remainingTasks);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "The scheduler priority queue was trimmed after dequeue threshold of {dequeuedSinceTrimThreshold} was exceeded.")]
        public partial void TrimmedExcess(int dequeuedSinceTrimThreshold);
    }
}

public interface IScheduler
{
    /// <summary>
    /// The count tasks in the queue.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Queues a Mediator command to run at a specific time.
    /// </summary>
    /// <param name="command">Mediator command to schedule.</param>
    /// <param name="executeAt">Time to execute the task at.</param>
    public void Schedule(ICommand command, DateTimeOffset executeAt);

    /// <summary>
    /// Queues a Mediator command to run at a specific time.
    /// </summary>
    /// <param name="command">Mediator command to schedule.</param>
    /// <param name="executionDelay">Delay for executing the task from now.</param>
    public void Schedule(ICommand command, TimeSpan executionDelay);

    /// <summary>
    /// Try to get the next scheduled task due to be executed.
    /// If a task exists, it is dequeued.
    /// </summary>
    /// <param name="task">Scheduled task.</param>
    /// <returns>Scheduled task to be executed or null.</returns>
    public bool TryGetNextTask([NotNullWhen(true)] out Func<CancellationToken, Task>? task);
}
