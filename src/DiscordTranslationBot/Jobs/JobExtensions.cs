using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DiscordTranslationBot.Jobs;

internal static class JobExtensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        return services.AddSingleton<IScheduler, Scheduler>().AddHostedService<SchedulerBackgroundService>();
    }
}
