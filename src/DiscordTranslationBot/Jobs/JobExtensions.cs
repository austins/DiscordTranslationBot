namespace DiscordTranslationBot.Jobs;

internal static class JobExtensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        return services.AddSingleton<IScheduler, Scheduler>().AddHostedService<SchedulerBackgroundService>();
    }
}
