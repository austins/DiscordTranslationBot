namespace DiscordTranslationBot.Jobs;

public sealed partial class SchedulerBackgroundService : BackgroundService
{
    /// <summary>
    /// Interval for getting and executing the next task in order to reduce overloading resources and deadlocking.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(1250);

    private readonly Log _log;
    private readonly IScheduler _scheduler;

    public SchedulerBackgroundService(IScheduler scheduler, ILogger<SchedulerBackgroundService> logger)
    {
        _scheduler = scheduler;
        _log = new Log(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Started();

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (_scheduler.TryGetNextTask(out var task))
                {
                    _log.TaskExecuting();

                    try
                    {
                        await task(stoppingToken);
                        _log.TaskExecuted();
                    }
                    catch (Exception ex)
                    {
                        _log.TaskFailed(ex);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _log.Stopped(_scheduler.Count);
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Started scheduler background service.")]
        public partial void Started();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Stopped scheduler background service with {remainingTasks} remaining tasks in the queue.")]
        public partial void Stopped(int remainingTasks);

        [LoggerMessage(Level = LogLevel.Information, Message = "Executing scheduled task...")]
        public partial void TaskExecuting();

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully executed scheduled task.")]
        public partial void TaskExecuted();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute scheduled task.")]
        public partial void TaskFailed(Exception ex);
    }
}
