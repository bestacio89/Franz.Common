#nullable enable

using System.Text.Json;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

internal sealed class CosmosSagaOutboxDocument
{
  public string Id { get; set; } = default!;
  public string TenantId { get; set; } = string.Empty;
  public string Payload { get; set; } = default!;
  public DateTime CreatedOn { get; set; }
  public DateTime? SentOn { get; set; }
  public DateTime? NextAttemptAt { get; set; }
  public Guid? LeaseOwnerId { get; set; }
  public DateTime? LeaseExpiresAt { get; set; }
  public bool IsDeadLetter { get; set; }

  public StoredMessage ToStored()
      => JsonSerializer.Deserialize<StoredMessage>(Payload)
          ?? throw new InvalidOperationException($"Cosmos outbox document '{Id}' has an invalid payload.");

  public static CosmosSagaOutboxDocument From(StoredMessage message, string tenantId)
      => new()
      {
        Id = message.Id.ToString("N"),
        TenantId = tenantId,
        Payload = JsonSerializer.Serialize(message),
        CreatedOn = message.CreatedOn,
        SentOn = message.SentOn,
        NextAttemptAt = message.NextAttemptAt,
        LeaseOwnerId = message.LeaseOwnerId,
        LeaseExpiresAt = message.LeaseExpiresAt,
        IsDeadLetter = message.IsDeadLetter
      };

  public void UpdatePayload(StoredMessage message)
  {
    Payload = JsonSerializer.Serialize(message);
    SentOn = message.SentOn;
    NextAttemptAt = message.NextAttemptAt;
    LeaseOwnerId = message.LeaseOwnerId;
    LeaseExpiresAt = message.LeaseExpiresAt;
    IsDeadLetter = message.IsDeadLetter;
  }
}
