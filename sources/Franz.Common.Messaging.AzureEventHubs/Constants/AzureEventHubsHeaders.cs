namespace Franz.Common.Messaging.AzureEventHubs.Constants;

public static class AzureEventHubsHeaders
{
  public const string PartitionId = "franz-eventhubs-partition-id";
  public const string SequenceNumber = "franz-eventhubs-sequence-number";
  public const string Offset = "franz-eventhubs-offset";
  public const string EnqueuedTime = "franz-eventhubs-enqueued-time";
}
