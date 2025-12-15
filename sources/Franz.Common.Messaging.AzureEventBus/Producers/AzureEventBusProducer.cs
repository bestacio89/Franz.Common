using Azure.Messaging.ServiceBus;
using Franz.Common.Errors;
using Franz.Common.Messaging;
using Franz.Common.Messaging.AzureEventBus.Mapping;

namespace Franz.Common.Messaging.AzureEventBus.Producers;

internal sealed class AzureEventBusProducer : IMessagingSender
{
  private readonly ServiceBusClient _client;
  private readonly IAzureEventBusMessageMapper _mapper;

  public AzureEventBusProducer(
      ServiceBusClient client,
      IAzureEventBusMessageMapper mapper)
  {
    _client = client;
    _mapper = mapper;
  }

  public async Task SendAsync(
      Message message,
      CancellationToken cancellationToken = default)
  {
    if (message is null)
      throw new ArgumentNullException(nameof(message));

    var destination = ResolveDestination(message);

    var sender = _client.CreateSender(destination);
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
    // 🔒 Franz convention:
    // destination is carried as a header or property.
    // Adjust this if you already have a standard key.
    if (message.Headers.TryGetValue("Destination", out var destination))
      return destination.ToString();

    throw new TechnicalException(
        "Franz Message does not contain a destination header for Azure Service Bus.");
  }
}
