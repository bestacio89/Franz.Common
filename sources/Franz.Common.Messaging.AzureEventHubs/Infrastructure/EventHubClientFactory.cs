using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Franz.Common.Messaging.AzureEventHubs.Configuration;

namespace Franz.Common.Messaging.AzureEventHubs.Infrastructure;

public sealed class EventHubsClientFactory
{
  private readonly AzureEventHubsOptions _options;

  public EventHubsClientFactory(AzureEventHubsOptions options)
  {
    _options = options;
    _options.Validate();
  }

  public EventHubProducerClient CreateProducer()
    => new(_options.ConnectionString, _options.EventHubName);
}
