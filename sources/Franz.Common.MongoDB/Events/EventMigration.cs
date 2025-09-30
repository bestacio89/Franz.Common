using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator.Messages;
using MongoDB.Driver;

namespace Franz.Common.MongoDB.Events
{
  public class EventMigration
  {
    private readonly IMongoCollection<IDomainEvent> _oldCollection;
    private readonly IMongoCollection<StoredEvent> _newCollection;

    public EventMigration(IMongoDatabase database)
    {
      _oldCollection = database.GetCollection<IDomainEvent>("Events");
      _newCollection = database.GetCollection<StoredEvent>("Events_New");
    }

    public async Task MigrateAsync()
    {
      var oldEvents = await _oldCollection.Find(FilterDefinition<IDomainEvent>.Empty).ToListAsync();

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
}
