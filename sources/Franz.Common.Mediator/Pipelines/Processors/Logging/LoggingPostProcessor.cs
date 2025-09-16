using Microsoft.Extensions.Logging;
namespace Franz.Common.Mediator.Pipelines.Processors.Logging
{
  public class LoggingPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
  {
    private readonly ILogger<LoggingPostProcessor<TRequest, TResponse>> _logger;

    public LoggingPostProcessor(ILogger<LoggingPostProcessor<TRequest, TResponse>> logger)
    {
      _logger = logger;
    }

    public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken = default)
    {
      _logger.LogInformation("[Post] {RequestName} produced response {Response}",
          typeof(TRequest).Name, response);
      return Task.CompletedTask;
    }
  }
}
