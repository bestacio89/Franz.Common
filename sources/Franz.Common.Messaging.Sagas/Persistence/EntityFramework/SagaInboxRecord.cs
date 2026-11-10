#nullable enable

using System.Text.Json;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

internal sealed class SagaInboxRecord
{
  public Guid MessageId { get; set; }
  public DateTime ProcessedOn { get; set; }
}
