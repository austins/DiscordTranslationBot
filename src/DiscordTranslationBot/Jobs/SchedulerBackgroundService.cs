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
    private readonly ILogger<SchedulerBackgroundService> _logger;
    private readonly Log _log;
    private readonly ActivitySource _activitySource;

    public SchedulerBackgroundService(
        IScheduler scheduler,
        ILogger<SchedulerBackgroundService> logger,
        Instrumentation instrumentation)
    {
        _scheduler = scheduler;
        _logger = logger;
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

                    using var traceLogScope =
                        _logger.BeginScope(new Dictionary<string, object> { ["trace.jobId"] = job.Id });

                    try
                    {
                        _log.TaskExecuting(job.Id, job.CommandName);
                        await job.Action(stoppingToken);
                        _log.TaskExecuted(job.Id, job.CommandName);
                    }
                    catch (Exception ex)
                    {
                        _log.TaskFailed(ex, job.Id);
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
            Message = "Stopped scheduler background service with {remainingTasks} remaining task(s) in the queue.")]
        public partial void Stopped(int remainingTasks);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executing scheduled task with ID {jobId} for command '{commandName}'...")]
        public partial void TaskExecuting(Guid jobId, string commandName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Successfully executed scheduled task with ID {jobId} for command '{commandName}'.")]
        public partial void TaskExecuted(Guid jobId, string commandName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute scheduled task ID {jobId}.")]
        public partial void TaskFailed(Exception ex, Guid jobId);
    }
}
