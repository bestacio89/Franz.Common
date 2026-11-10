#nullable enable

using System.Text.Json;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

internal sealed class SagaOutboxRecord
{
  public Guid Id { get; set; }
  public string Body { get; set; } = string.Empty;
  public string HeadersJson { get; set; } = "{}";
  public string PropertiesJson { get; set; } = "{}";
  public Guid? CorrelationId { get; set; }
  public string? MessageType { get; set; }
  public DateTime CreatedOn { get; set; }
  public DateTime? SentOn { get; set; }
  public int RetryCount { get; set; }
  public string? LastError { get; set; }
  public DateTime? LastTriedOn { get; set; }
  public DateTime? NextAttemptAt { get; set; }
  public Guid? LeaseOwnerId { get; set; }
  public DateTime? LeaseExpiresAt { get; set; }
  public bool IsDeadLetter { get; set; }

  public static SagaOutboxRecord From(Message message)
      => From(message.ToStored());

  public static SagaOutboxRecord From(StoredMessage stored)
  {
    ArgumentNullException.ThrowIfNull(stored);
    return new SagaOutboxRecord
    {
      Id = stored.Id,
      Body = stored.Body,
      HeadersJson = JsonSerializer.Serialize(stored.Headers),
      PropertiesJson = JsonSerializer.Serialize(stored.Properties),
      CorrelationId = stored.CorrelationId,
      MessageType = stored.MessageType,
      CreatedOn = stored.CreatedOn,
      NextAttemptAt = stored.NextAttemptAt
    };
  }

  public StoredMessage ToStored()
  {
    var headers = JsonSerializer.Deserialize<Dictionary<string, string[]>>(HeadersJson)
        ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(PropertiesJson)
        ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    return new StoredMessage
    {
      Id = Id,
      Body = Body,
      Headers = headers,
      Properties = properties.ToDictionary(
          pair => pair.Key,
          pair => (object)pair.Value,
          StringComparer.OrdinalIgnoreCase),
      CorrelationId = CorrelationId,
      MessageType = MessageType,
      CreatedOn = CreatedOn,
      SentOn = SentOn,
      RetryCount = RetryCount,
      LastError = LastError,
      LastTriedOn = LastTriedOn,
      NextAttemptAt = NextAttemptAt,
      LeaseOwnerId = LeaseOwnerId,
      LeaseExpiresAt = LeaseExpiresAt,
      IsDeadLetter = IsDeadLetter
    };
  }
}
