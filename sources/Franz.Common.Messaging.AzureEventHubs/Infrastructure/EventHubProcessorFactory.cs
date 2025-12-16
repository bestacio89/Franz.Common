using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Franz.Common.Messaging.AzureEventHubs.Configuration;

namespace Franz.Common.Messaging.AzureEventHubs.Infrastructure;

public sealed class EventHubsProcessorFactory
{
  private readonly AzureEventHubsOptions _options;

  public EventHubsProcessorFactory(AzureEventHubsOptions options)
  {
    _options = options;
    _options.Validate();
  }

  public EventProcessorClient CreateProcessor()
  {
    var blobClient = new BlobContainerClient(
        _options.BlobConnectionString,
        _options.BlobContainerName);

    blobClient.CreateIfNotExists();

    return new EventProcessorClient(
        blobClient,
        _options.ConsumerGroup,
        _options.ConnectionString,
        _options.EventHubName);
  }
}
