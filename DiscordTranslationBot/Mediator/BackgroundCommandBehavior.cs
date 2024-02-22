using AsyncAwaitBestPractices;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator behavior for background commands.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed partial class BackgroundCommandBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundCommandBehavior{TRequest,TResponse}" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public BackgroundCommandBehavior(ILogger<BackgroundCommandBehavior<TRequest, TResponse>> logger)
    {
        _log = new Log(logger);
    }

    /// <summary>
    /// Runs an incoming request in the background if it is an <see cref="IBackgroundCommand" />.
    /// Otherwise, it continues on to the next action in the pipeline.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="next">
    /// Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the
    /// handler.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    /// <exception cref="InvalidOperationException">If the request is a background command and it has an invalid delay.</exception>
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IBackgroundCommand command)
        {
            return next();
        }

        if (command.Delay is not null && command.Delay.Value <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Delay must be greater than zero or null.");
        }

        ExecuteCommandAsync().SafeFireAndForget(ex => _log.FailureInRequestHandler(ex));

        var commandName = command.GetType().Name;
        if (command.Delay is null)
        {
            _log.BackgroundCommandSent(commandName);
        }
        else
        {
            _log.BackgroundCommandSentWithDelay(commandName, command.Delay.Value.TotalSeconds);
        }

        return (Unit.Task as Task<TResponse>)!;

        async Task ExecuteCommandAsync()
        {
            if (command.Delay is not null)
            {
                await Task.Delay(command.Delay.Value, cancellationToken);
            }

            await next();
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<BackgroundCommandBehavior<TRequest, TResponse>> _logger;

        public Log(ILogger<BackgroundCommandBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Sent background command '{commandName}'.")]
        public partial void BackgroundCommandSent(string commandName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Sent background command '{commandName}' with {delayinSeconds}s delay.")]
        public partial void BackgroundCommandSentWithDelay(string commandName, double delayInSeconds);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "An exception has occurred in a request handler for a background command.")]
        public partial void FailureInRequestHandler(Exception ex);
    }
}
