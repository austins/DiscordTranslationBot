using DiscordTranslationBot.Telemetry;
using System.Diagnostics;

namespace DiscordTranslationBot.Jobs;

internal sealed partial class SchedulerBackgroundService : BackgroundService
{
    /// <summary>
    /// Interval for getting and executing the next task in order to reduce overloading resources and deadlocking.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(1250);

    private readonly IScheduler _scheduler;
    private readonly Log _log;
    private readonly ActivitySource _activitySource;

    public SchedulerBackgroundService(
        IScheduler scheduler,
        ILogger<SchedulerBackgroundService> logger,
        Instrumentation instrumentation)
    {
        _scheduler = scheduler;
        _log = new Log(logger);
        _activitySource = instrumentation.ActivitySource;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Started();

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var job = await _scheduler.GetNextJobDueAsync(stoppingToken);
                if (job != null)
                {
                    // Start a trace scope for this job execution.
                    using var traceActivity = _activitySource.StartActivity(
                        $"{nameof(SchedulerBackgroundService)}.{nameof(ExecuteAsync)}: {{commandName}}");

                    traceActivity?.SetTag("commandName", job.CommandName);

                    try
                    {
                        _log.TaskExecuting(job.CommandName);
                        await job.Action(stoppingToken);
                        _log.TaskExecuted(job.CommandName);
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

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Started scheduler background service.")]
        public partial void Started();

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Stopped scheduler background service with {remainingTasks} remaining tasks in the queue.")]
        public partial void Stopped(int remainingTasks);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executing scheduled task for command '{commandName}'...")]
        public partial void TaskExecuting(string commandName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Successfully executed scheduled task for command '{commandName}'.")]
        public partial void TaskExecuted(string commandName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute scheduled task.")]
        public partial void TaskFailed(Exception ex);
    }
}
