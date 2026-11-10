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

internal sealed class CosmosSagaInboxDocument
{
  public string Id { get; set; } = default!;
  public string TenantId { get; set; } = string.Empty;
  public DateTime ProcessedOn { get; set; }
}
