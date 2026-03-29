#nullable enable
using Azure.Messaging.ServiceBus;
using Franz.Common.Errors;
using Franz.Common.Messaging.AzureEventBus.Mapping;
using Franz.Common.Messaging.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.AzureEventBus.Producers;

public sealed class AzureEventBusProducer : IMessagingSender, IDisposable, IAsyncDisposable
{
  private readonly ServiceBusClient _client;
  private readonly IAzureEventBusMessageMapper _mapper;
  private int _disposed = 0;

  public AzureEventBusProducer(
      ServiceBusClient client,
      IAzureEventBusMessageMapper mapper)
  {
    _client = client;
    _mapper = mapper;
  }

  public async ValueTask SendAsync(
      Message message,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (Volatile.Read(ref _disposed) == 1)
      throw new ObjectDisposedException(nameof(AzureEventBusProducer));

    var destination = ResolveDestination(message);

    // Architect Note: ServiceBusClient.CreateSender returns a sender that 
    // should be cached for the lifetime of the client or disposed. 
    // For Franz implementation, we dispose the specific sender after use 
    // unless we move to a ConcurrentDictionary cache for high-throughput.
    await using var sender = _client.CreateSender(destination);
    var sbMessage = _mapper.ToServiceBusMessage(message);

    try
    {
      await sender.SendMessageAsync(sbMessage, cancellationToken);
    }
    catch (Exception ex)
    {
      throw new TechnicalException(
          $"Failed to send message to Azure Service Bus entity '{destination}'.", ex);
    }
  }

  private static string ResolveDestination(Message message)
  {
    if (message.Headers.TryGetValue("Destination", out var values))
    {
      var destination = values.FirstOrDefault();
      if (!string.IsNullOrWhiteSpace(destination))
        return destination;
    }

    throw new TechnicalException(
        "Franz Message does not contain a valid destination header for Azure Service Bus.");
  }

  public async ValueTask DisposeAsync()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

    // Dispose the client if this producer owns its lifetime. 
    // If the client is injected via DI as a singleton, we might 
    // only want to dispose internal senders if they were cached.
    await _client.DisposeAsync();

    GC.SuppressFinalize(this);
  }

  public void Dispose()
  {
    if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

    // Sync disposal for ServiceBusClient
    _client.DisposeAsync().GetAwaiter().GetResult();

    GC.SuppressFinalize(this);
  }
}