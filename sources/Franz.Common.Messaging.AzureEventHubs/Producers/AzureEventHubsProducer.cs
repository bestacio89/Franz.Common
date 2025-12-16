using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Franz.Common.Messaging;
using Franz.Common.Messaging.AzureEventHubs.Infrastructure;
using Franz.Common.Messaging.AzureEventHubs.Serialization;

namespace Franz.Common.Messaging.AzureEventHubs.Producers;

public sealed class AzureEventHubsProducer : IMessagingSender, IAsyncDisposable
{
  private readonly EventHubProducerClient _producer;
  private readonly AzureEventHubsMessageSerializer _serializer;

  public AzureEventHubsProducer(
      EventHubsClientFactory factory,
      AzureEventHubsMessageSerializer serializer)
  {
    _producer = factory.CreateProducer();
    _serializer = serializer;
  }

  public async Task SendAsync(
      Message message,
      CancellationToken cancellationToken = default)
  {
    using var batch = await _producer.CreateBatchAsync(cancellationToken);

    var body = _serializer.Serialize(message.Body ?? string.Empty);

    var evt = new EventData(body)
    {
      MessageId = message.Id,
      CorrelationId = message.CorrelationId
    };

    if (!batch.TryAdd(evt))
      throw new InvalidOperationException("Event too large for Event Hub batch.");

    await _producer.SendAsync(batch, cancellationToken);
  }

  public async ValueTask DisposeAsync()
    => await _producer.DisposeAsync();
}
