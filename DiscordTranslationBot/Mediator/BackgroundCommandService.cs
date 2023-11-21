using AsyncAwaitBestPractices;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Service that executes Mediator commands in the background.
/// </summary>
public sealed partial class BackgroundCommandService : IBackgroundCommandService
{
    private readonly Log _log;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundCommandService" /> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public BackgroundCommandService(IMediator mediator, ILogger<BackgroundCommandService> logger)
    {
        _mediator = mediator;
        _log = new Log(logger);
    }

    /// <inheritdoc cref="IBackgroundCommandService.Schedule" />
    public void Schedule(IBackgroundCommand request, CancellationToken cancellationToken)
    {
        if (request.Delay != null && request.Delay.Value <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Delay must be greater than zero or null.");
        }

        SendAsync().SafeFireAndForget(ex => _log.FailureInRequestHandler(ex));

        async Task SendAsync()
        {
            if (request.Delay != null)
            {
                await Task.Delay(request.Delay.Value, cancellationToken);
            }

            await _mediator.Send(request, cancellationToken);
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<BackgroundCommandService> _logger;

        public Log(ILogger<BackgroundCommandService> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "An exception has occurred in a request handler.")]
        public partial void FailureInRequestHandler(Exception ex);
    }
}
