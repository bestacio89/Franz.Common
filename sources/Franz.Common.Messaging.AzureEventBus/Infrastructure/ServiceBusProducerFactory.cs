using Azure.Messaging.ServiceBus;
using Franz.Common.Messaging.AzureEventBus.Configuration;

namespace Franz.Common.Messaging.AzureEventBus.Infrastructure;

internal sealed class ServiceBusProducerFactory
{
  private readonly ServiceBusSenderFactory _senderFactory;
  private readonly AzureEventBusOptions _options;

  public ServiceBusProducerFactory(
      ServiceBusSenderFactory senderFactory,
      AzureEventBusOptions options)
  {
    _senderFactory = senderFactory;
    _options = options;
  }

  public ServiceBusSender CreateDefault()
  {
    if (string.IsNullOrWhiteSpace(_options.DefaultTopic))
      throw new InvalidOperationException(
          "AzureEventBusOptions.DefaultTopic must be configured to create a default producer.");

    return _senderFactory.Create(_options.DefaultTopic);
  }

  public ServiceBusSender Create(string entityName)
  {
    return _senderFactory.Create(entityName);
  }
}
