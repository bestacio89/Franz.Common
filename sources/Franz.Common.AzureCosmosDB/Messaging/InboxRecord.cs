namespace Franz.Common.Messaging;

/// <summary>
/// Simple EF entity to track processed messages for inbox idempotency.
/// </summary>
public class InboxRecord
{
  public string MessageId { get; set; } = default!;
  public DateTime ProcessedOn { get; set; } = DateTime.UtcNow;
}
