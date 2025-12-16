using Azure.Messaging.ServiceBus;
using Franz.Common.Messaging.AzureEventBus.Configuration;

namespace Franz.Common.Messaging.AzureEventBus.Infrastructure;

public sealed class ServiceBusClientFactory
{
  private readonly AzureEventBusOptions _options;

  public ServiceBusClientFactory(AzureEventBusOptions options)
  {
    _options = options;
    _options.Validate();
  }

  public ServiceBusClient Create()
  {
    // Keep it simple & predictable. If later you want token credentials, add a second ctor.
    return new ServiceBusClient(_options.ConnectionString);
  }
}
