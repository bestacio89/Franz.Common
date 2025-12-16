using Azure.Messaging.ServiceBus;
using Franz.Common.Messaging.AzureEventBus.Configuration;

namespace Franz.Common.Messaging.AzureEventBus.Infrastructure;

public sealed class ServiceBusSenderFactory
{
  private readonly ServiceBusClient _client;
  private readonly AzureEventBusOptions _options;

  public ServiceBusSenderFactory(
      ServiceBusClient client,
      AzureEventBusOptions options)
  {
    _client = client;
    _options = options;
  }

  public ServiceBusSender Create(string entityName)
  {
    if (string.IsNullOrWhiteSpace(entityName))
      throw new ArgumentException("Service Bus entity name is required.", nameof(entityName));

    return _client.CreateSender(entityName);
  }
}
