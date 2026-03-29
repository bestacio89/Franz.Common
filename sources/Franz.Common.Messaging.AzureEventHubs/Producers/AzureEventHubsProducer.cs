#nullable enable
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Franz.Common.Messaging.AzureEventHubs.Infrastructure;
using Franz.Common.Messaging.AzureEventHubs.Serialization;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.AzureEventHubs.Producers;

public sealed class AzureEventHubsProducer : IMessagingSender, IAsyncDisposable, IDisposable
{
  private readonly EventHubProducerClient _producer;
  private readonly AzureEventHubsMessageSerializer _serializer;
  private int _disposed = 0;

  public AzureEventHubsProducer(
      EventHubsClientFactory factory,
      AzureEventHubsMessageSerializer serializer)
  {
    _producer = factory.CreateProducer();
    _serializer = serializer;
  }

  public async ValueTask SendAsync(
      Message message,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (Volatile.Read(ref _disposed) == 1)
      throw new ObjectDisposedException(nameof(AzureEventHubsProducer));

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

  public async ValueTask DisposeAsync()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

    await _producer.DisposeAsync();

    GC.SuppressFinalize(this);
  }

  public void Dispose()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

    // EventHubProducerClient.DisposeAsync() is the preferred way to close the connection.
    // In a synchronous Dispose call, we must block to ensure resources are released.
    _producer.DisposeAsync().AsTask().GetAwaiter().GetResult();

    GC.SuppressFinalize(this);
  }
}