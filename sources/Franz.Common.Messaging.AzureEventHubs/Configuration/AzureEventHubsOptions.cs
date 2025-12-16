using Franz.Common.Errors;

namespace Franz.Common.Messaging.AzureEventHubs.Configuration;

public sealed class AzureEventHubsOptions
{
  /// <summary>Event Hubs connection string.</summary>
  public string ConnectionString { get; set; } = string.Empty;

  /// <summary>Name of the Event Hub.</summary>
  public string EventHubName { get; set; } = string.Empty;

  /// <summary>Consumer group name.</summary>
  public string ConsumerGroup { get; set; } = "$Default";

  /// <summary>Blob Storage connection string for checkpoints.</summary>
  public string BlobConnectionString { get; set; } = string.Empty;

  /// <summary>Blob container name used for checkpoints.</summary>
  public string BlobContainerName { get; set; } = "eventhub-checkpoints";

  /// <summary>Maximum events per partition per batch.</summary>
  public int MaxBatchSize { get; set; } = 100;

  /// <summary>Whether processing should start from the beginning.</summary>
  public bool StartFromBeginning { get; set; } = false;

  internal void Validate()
  {
    if (string.IsNullOrWhiteSpace(ConnectionString))
      throw new TechnicalException("AzureEventHubsOptions.ConnectionString is required.");

    if (string.IsNullOrWhiteSpace(EventHubName))
      throw new TechnicalException("AzureEventHubsOptions.EventHubName is required.");

    if (string.IsNullOrWhiteSpace(BlobConnectionString))
      throw new TechnicalException("AzureEventHubsOptions.BlobConnectionString is required.");

    if (string.IsNullOrWhiteSpace(BlobContainerName))
      throw new TechnicalException("AzureEventHubsOptions.BlobContainerName is required.");

    if (MaxBatchSize <= 0)
      throw new TechnicalException("AzureEventHubsOptions.MaxBatchSize must be > 0.");
  }
}
