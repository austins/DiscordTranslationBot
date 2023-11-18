using AsyncAwaitBestPractices;

namespace DiscordTranslationBot.Mediator;

public sealed partial class BackgroundCommandService : IBackgroundCommandService
{
    private readonly Log _log;
    private readonly IMediator _mediator;

    public BackgroundCommandService(IMediator mediator, ILogger<BackgroundCommandService> logger)
    {
        _mediator = mediator;
        _log = new Log(logger);
    }

    public void Invoke(IRequest request, CancellationToken cancellationToken)
    {
        _mediator.Send(request, cancellationToken).SafeFireAndForget(ex => _log.FailureInRequestHandler(ex));
    }

    public void Schedule(IRequest request, TimeSpan delay, CancellationToken cancellationToken)
    {
        SendAsync().SafeFireAndForget(ex => _log.FailureInRequestHandler(ex));
        return;

        async Task SendAsync()
        {
            await Task.Delay(delay, cancellationToken);
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
