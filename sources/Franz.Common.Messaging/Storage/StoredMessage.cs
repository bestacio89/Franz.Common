namespace Franz.Common.Messaging
{
  public class StoredMessage
  {
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Body { get; set; } = default!;

    public IDictionary<string, string[]> Headers { get; set; } = new Dictionary<string, string[]>();

    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

    public string? CorrelationId { get; set; }

    public string? MessageType { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? SentOn { get; set; }

    // 🚀 New retry & DLQ fields
    public int RetryCount { get; set; } = 0;

    public string? LastError { get; set; }

    public DateTime? LastTriedOn { get; set; }
  }
}
