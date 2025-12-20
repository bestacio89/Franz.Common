using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.AzureEventHubs.Infrastructure;
using Franz.Common.Messaging.AzureEventHubs.Mapping;
using Franz.Common.Messaging.AzureEventHubs.Serialization;
using Microsoft.Extensions.Logging;

namespace Franz.Common.Messaging.AzureEventHubs.Consumers;

public sealed class AzureEventHubsProcessor
{
  private readonly EventProcessorClient _processor;
  private readonly AzureEventHubsMessageMapper _mapper;
  private readonly AzureEventHubsMessageSerializer _serializer;
  private readonly IDispatcher _dispatcher;
  private readonly ILogger<AzureEventHubsProcessor> _logger;

  public AzureEventHubsProcessor(
      EventHubsProcessorFactory factory,
      AzureEventHubsMessageMapper mapper,
      AzureEventHubsMessageSerializer serializer,
      IDispatcher dispatcher,
      ILogger<AzureEventHubsProcessor> logger)
  {
    _processor = factory.CreateProcessor(); // 👈 create once
    _mapper = mapper;
    _serializer = serializer;
    _dispatcher = dispatcher;
    _logger = logger;

    _processor.ProcessEventAsync += OnEventAsync;
    _processor.ProcessErrorAsync += OnErrorAsync;
  }

  public Task StartAsync(CancellationToken ct = default)
    => _processor.StartProcessingAsync(ct);

  public Task StopAsync(CancellationToken ct = default)
    => _processor.StopProcessingAsync(ct);

  private async Task OnEventAsync(ProcessEventArgs args)
  {
    var body = _serializer.Deserialize(args.Data.EventBody.ToArray());
    var message = _mapper.FromEvent(args, body);

    await _dispatcher.PublishNotificationAsync(message, args.CancellationToken);
    await args.UpdateCheckpointAsync(args.CancellationToken);
  }

  private Task OnErrorAsync(ProcessErrorEventArgs args)
  {
    _logger.LogError(
        args.Exception,
        "🔥 Event Hubs processing error. Partition={PartitionId}",
        args.PartitionId);

    return Task.CompletedTask;
  }


}
