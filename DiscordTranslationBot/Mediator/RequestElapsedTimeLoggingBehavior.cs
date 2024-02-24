using System.Diagnostics;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator behavior for logging elapsed time of requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed partial class RequestElapsedTimeLoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestElapsedTimeLoggingBehavior{TRequest,TResponse}" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public RequestElapsedTimeLoggingBehavior(ILogger<RequestElapsedTimeLoggingBehavior<TRequest, TResponse>> logger)
    {
        _log = new Log(logger);
    }

    /// <summary>
    /// Executes requests and logs elapsed time of requests.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="next">
    /// Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the
    /// handler.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = request.GetType().Name;
        _log.RequestExecuting(requestName);

        var stopwatch = Stopwatch.StartNew();
        var result = await next();
        stopwatch.Stop();

        _log.RequestExecuted(requestName, stopwatch.ElapsedMilliseconds);

        return result;
    }

    private sealed partial class Log
    {
        private readonly ILogger<RequestElapsedTimeLoggingBehavior<TRequest, TResponse>> _logger;

        public Log(ILogger<RequestElapsedTimeLoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Executing request '{requestName}'...")]
        public partial void RequestExecuting(string requestName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executed request '{requestName}'. Elapsed time: {elapsedMilliseconds}ms.")]
        public partial void RequestExecuted(string requestName, long elapsedMilliseconds);
    }
}
