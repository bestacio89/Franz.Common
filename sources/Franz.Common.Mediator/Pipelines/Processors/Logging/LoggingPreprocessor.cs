using Microsoft.Extensions.Logging;

namespace Franz.Common.Mediator.Pipelines.Processors.Logging
{
  public class LoggingPreProcessor<TRequest> : IPreProcessor<TRequest>
  {
    private readonly ILogger<LoggingPreProcessor<TRequest>> _logger;

    public LoggingPreProcessor(ILogger<LoggingPreProcessor<TRequest>> logger)
    {
      _logger = logger;
    }

    public Task ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
    {
      _logger.LogInformation("[Pre] Handling {RequestName}", typeof(TRequest).Name);
      return Task.CompletedTask;
    }
  }
}
