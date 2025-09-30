using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Dispatchers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Franz.Common.MongoDB.Events;
using Franz.Common.Business.Events;

namespace Franz.Common.MongoDB.Repositories.Implementations
{
  /// <summary>
  /// MongoDB implementation of <see cref="IAggregateRepository{TAggregate, TEvent}"/>
  /// using event-sourcing with a StoredEvent envelope.
  /// </summary>
  public class MongoAggregateRepository<TAggregate, TEvent>
      : IAggregateRepository<TAggregate, TEvent>
      where TAggregate : AggregateRoot<TEvent>
      where TEvent : IDomainEvent
  {
    private readonly IMongoCollection<StoredEvent> _eventCollection;
    private readonly IDispatcher _mediator;

    public MongoAggregateRepository(IMongoDatabase database, IDispatcher mediator)
    {
      _eventCollection = database.GetCollection<StoredEvent>("Events");
      _mediator = mediator;
    }

    /// <summary>
    /// Loads an aggregate by replaying its domain events.
    /// Throws <see cref="NotFoundException"/> if no events are found.
    /// </summary>
    public async Task<TAggregate> GetByIdAsync(Guid id)
    {
      var storedEvents = await _eventCollection
          .Find(Builders<StoredEvent>.Filter.Eq(e => e.AggregateId, id))
          .SortBy(e => e.OccurredOn)
          .ToListAsync();

      if (!storedEvents.Any())
        throw new NotFoundException($"{typeof(TAggregate).Name} with ID '{id}' was not found.");

      var domainEvents = storedEvents.Select(se =>
      {
        var type = Type.GetType(se.EventType, throwOnError: true)!;
        return (TEvent)se.DeserializePayload(type);
      }).ToList();

      var aggregate = (TAggregate)Activator.CreateInstance(typeof(TAggregate), true)!;
      aggregate.ReplayEvents(domainEvents);
      return aggregate;
    }

    /// <summary>
    /// Persists uncommitted domain events and publishes them via mediator.
    /// </summary>
    public async Task SaveAsync(TAggregate aggregate)
    {
      var uncommitted = aggregate.GetUncommittedChanges().Cast<TEvent>().ToList();
      if (!uncommitted.Any()) return;

      var storedEvents = uncommitted.Select(ev => new StoredEvent
      {
        EventId = ev.EventId,
        AggregateId = (Guid)ev.AggregateId,
        AggregateType = ev.AggregateType,
        EventType = ev.GetType().AssemblyQualifiedName!,
        OccurredOn = ev.OccurredOn,
        CorrelationId = ev.CorrelationId,
        Payload = JsonSerializer.Serialize(ev)
      }).ToList();

      await _eventCollection.InsertManyAsync(storedEvents);
      aggregate.MarkChangesAsCommitted();

      foreach (var ev in uncommitted)
        await _mediator.PublishEventAsync(ev);
    }

    /// <summary>
    /// Deletes all events belonging to the given aggregate.
    /// </summary>
    public Task DeleteAsync(Guid id) =>
        _eventCollection.DeleteManyAsync(Builders<StoredEvent>.Filter.Eq(e => e.AggregateId, id));
  }

  /// <summary>
  /// Exception thrown when a requested aggregate is not found in the event store.
  /// </summary>
  public class NotFoundException : Exception
  {
    public NotFoundException(string message) : base(message) { }
  }
}
