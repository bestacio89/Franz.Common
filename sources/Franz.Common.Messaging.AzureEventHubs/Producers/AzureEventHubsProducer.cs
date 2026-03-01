#nullable enable
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Franz.Common.Messaging.AzureEventHubs.Infrastructure;
using Franz.Common.Messaging.AzureEventHubs.Serialization;
using Franz.Common.Messaging.Messages;

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
    if (message is null) throw new ArgumentNullException(nameof(message));

    using var batch = await _producer.CreateBatchAsync(cancellationToken);

    // Serialize the body
    var body = _serializer.Serialize(message.Body ?? string.Empty);

    // We use .ToString() here for the native Azure properties,
    // ensuring the chronological identity is preserved across the wire.
    var evt = new EventData(body)
    {
      MessageId = message.Id.ToString(),
      CorrelationId = message.CorrelationId.ToString()
    };

    // Standard Practice: Also duplicate the CorrelationId in the Properties bag 
    // for easier filtering in Stream Analytics or Azure Functions.
    evt.Properties[nameof(Message.CorrelationId)] = message.CorrelationId.ToString();

    if (!batch.TryAdd(evt))
    {
      // Log contextual error with the ID before throwing
      throw new InvalidOperationException($"Event {message.Id} is too large for Event Hub batch.");
    }

    await _producer.SendAsync(batch, cancellationToken);
  }

  public async ValueTask DisposeAsync() => await _producer.DisposeAsync();
}