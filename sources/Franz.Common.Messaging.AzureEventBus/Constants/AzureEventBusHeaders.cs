namespace Franz.Common.Messaging.AzureEventBus.Constants;

/// <summary>
/// Canonical Franz headers for Azure Service Bus.
/// Stored in ServiceBusMessage.ApplicationProperties (except CorrelationId/MessageId where Azure has native fields).
/// </summary>
public static class AzureEventBusHeaders
{
  public const string Prefix = "franz-";

  public const string EventType = Prefix + "event-type";
  public const string SchemaVersion = Prefix + "schema-version";
  public const string TenantId = Prefix + "tenant-id";
  public const string CausationId = Prefix + "causation-id";
  public const string PublishedAtUtc = Prefix + "published-at-utc";

  // Useful operational headers (optional but standardized)
  public const string Producer = Prefix + "producer";
  public const string Environment = Prefix + "environment";
}
