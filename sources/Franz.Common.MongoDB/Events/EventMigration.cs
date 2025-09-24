using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.MongoDB.Events;
using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Messages;
using Franz.Common.MongoDB.Events;
using global::MongoDB.Driver;
using System.Text.Json;

public class EventMigration
{
  private readonly IMongoCollection<BaseDomainEvent> _oldCollection;
  private readonly IMongoCollection<StoredEvent> _newCollection;

  public EventMigration(IMongoDatabase database)
  {
    _oldCollection = database.GetCollection<BaseDomainEvent>("Events");
    _newCollection = database.GetCollection<StoredEvent>("Events_New");
  }

  public async Task MigrateAsync()
  {
    var oldEvents = await _oldCollection.Find(FilterDefinition<BaseDomainEvent>.Empty).ToListAsync();

    var storedEvents = oldEvents.Select(ev => new StoredEvent
    {
      EventId = ev.EventId,
      AggregateId = (Guid)ev.AggregateId,
      AggregateType = ev.AggregateType,
      EventType = ev.GetType().AssemblyQualifiedName!,
      OccurredOn = ev.OccurredOn,
      CorrelationId = ev.CorrelationId,
      Payload = JsonSerializer.Serialize(ev)
    }).ToList();

    if (storedEvents.Any())
      await _newCollection.InsertManyAsync(storedEvents);

    Console.WriteLine($"Migrated {storedEvents.Count} events.");
  }
}

