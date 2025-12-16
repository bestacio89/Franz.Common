using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.AzureEventBus.Consumers;

/// <summary>
/// Coordinates one or more AzureEventBusProcessors.
/// Represents the logical Azure consumer for the service.
/// </summary>
public sealed class AzureEventBusConsumer : IAsyncDisposable
{
  private readonly IReadOnlyCollection<AzureEventBusProcessor> _processors;
  private readonly ILogger<AzureEventBusConsumer> _logger;

  public AzureEventBusConsumer(
      IEnumerable<AzureEventBusProcessor> processors,
      ILogger<AzureEventBusConsumer> logger)
  {
    _processors = processors.ToList().AsReadOnly();
    _logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken = default)
  {
    _logger.LogInformation(
        "🚀 Starting AzureEventBusConsumer with {ProcessorCount} processor(s)",
        _processors.Count);

    foreach (var processor in _processors)
    {
      await processor.StartAsync(cancellationToken);
    }
  }

  public async Task StopAsync(CancellationToken cancellationToken = default)
  {
    _logger.LogInformation(
        "🛑 Stopping AzureEventBusConsumer");

    foreach (var processor in _processors)
    {
      await processor.StopAsync(cancellationToken);
    }
  }

  public async ValueTask DisposeAsync()
  {
    foreach (var processor in _processors)
    {
      await processor.DisposeAsync();
    }
  }
}
