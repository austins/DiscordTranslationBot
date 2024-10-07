namespace DiscordTranslationBot.Jobs;

public sealed partial class SchedulerBackgroundService : BackgroundService
{
    private readonly Log _log;
    private readonly IScheduler _scheduler;

    public SchedulerBackgroundService(IScheduler scheduler, ILogger<SchedulerBackgroundService> logger)
    {
        _scheduler = scheduler;
        _log = new Log(logger);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _log.Starting();
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _log.Stopping(_scheduler.Count);
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
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

            // Wait some time before checking the queue again to reduce overloading CPU resources.
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Starting scheduler background service...")]
        public partial void Starting();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Stopping scheduler background service with {remainingTasks} remaining tasks in the queue...")]
        public partial void Stopping(int remainingTasks);

        [LoggerMessage(Level = LogLevel.Information, Message = "Executing scheduled task...")]
        public partial void TaskExecuting();

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully executed scheduled task.")]
        public partial void TaskExecuted();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute scheduled task.")]
        public partial void TaskFailed(Exception ex);
    }
}
