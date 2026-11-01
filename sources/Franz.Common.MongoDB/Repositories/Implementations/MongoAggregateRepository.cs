using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Dispatchers;
using MongoDB.Driver;
using System.Text.Json;
using Franz.Common.MongoDB.Events;
using Franz.Common.Business.Events;
using Franz.Common.MultiTenancy;

namespace Franz.Common.MongoDB.Repositories.Implementations
{
  /// <summary>
  /// MongoDB event-store repository. When a tenant execution accessor is registered,
  /// events are stamped and queried by tenant. Without an accessor, the repository
  /// operates only on legacy/non-tenant events whose TenantId is null.
  /// </summary>
  public class MongoAggregateRepository<TAggregate, TEvent>
      : IAggregateRepository<TAggregate, TEvent>
      where TAggregate : AggregateRoot<TEvent>
      where TEvent : IDomainEvent
  {
    private readonly IMongoCollection<StoredEvent> _eventCollection;
    private readonly IDispatcher _mediator;
    private readonly ITenantExecutionContextAccessor? _tenantContextAccessor;

    /// <summary>
    /// Retains the original constructor signature for existing consumers.
    /// </summary>
    public MongoAggregateRepository(IMongoDatabase database, IDispatcher mediator)
        : this(database, mediator, null)
    {
    }

    public MongoAggregateRepository(
        IMongoDatabase database,
        IDispatcher mediator,
        ITenantExecutionContextAccessor? tenantContextAccessor)
    {
      ArgumentNullException.ThrowIfNull(database);
      _eventCollection = database.GetCollection<StoredEvent>("Events");
      _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
      _tenantContextAccessor = tenantContextAccessor;
    }

    /// <summary>
    /// Loads an aggregate by replaying only events owned by the current tenant.
    /// Without a tenant accessor, only non-tenant legacy events are visible.
    /// </summary>
    public async Task<TAggregate?> GetByIdAsync(Guid id)
    {
      var tenantId = GetTenantIdOrThrow();
      var filter = TenantFilter(tenantId)
          & Builders<StoredEvent>.Filter.Eq(e => e.AggregateId, id);

      var storedEvents = await _eventCollection
          .Find(filter)
          .SortBy(e => e.OccurredOn)
          .ToListAsync()
          .ConfigureAwait(false);

      if (!storedEvents.Any())
        throw new NotFoundException(
            $"{typeof(TAggregate).Name} with ID '{id}' was not found for the current tenant.");

      var domainEvents = storedEvents.Select(se =>
      {
        var type = Type.GetType(se.EventType, throwOnError: true)!;
        return (TEvent)se.DeserializePayload(type);
      }).ToList();

      var aggregate = (TAggregate)Activator.CreateInstance(typeof(TAggregate), true)!;
      aggregate.Rehydrate(id, domainEvents);
      return aggregate;
    }

    /// <summary>
    /// Persists uncommitted events with the current tenant identity and publishes them.
    /// The tenant identity is derived from the validated execution context, never event metadata.
    /// </summary>
    public async Task SaveAsync(TAggregate aggregate)
    {
      ArgumentNullException.ThrowIfNull(aggregate);
      var tenantId = GetTenantIdOrThrow();
      var uncommitted = aggregate.GetUncommittedChanges().Cast<TEvent>().ToList();
      if (!uncommitted.Any()) return;

      var aggregateId = aggregate.Id;
      if (aggregateId == Guid.Empty)
        throw new InvalidOperationException("A tenant-owned aggregate must have a non-empty identifier.");

      foreach (var domainEvent in uncommitted)
      {
        if (domainEvent.AggregateId != aggregateId)
          throw new InvalidOperationException(
              "A domain event cannot be persisted into an aggregate stream with a different identifier.");
      }

      var storedEvents = uncommitted.Select(ev => new StoredEvent
      {
        EventId = ev.EventId,
        AggregateId = aggregateId,
        TenantId = tenantId,
        AggregateType = ev.AggregateType,
        EventType = ev.GetType().AssemblyQualifiedName!,
        OccurredOn = ev.OccurredOn,
        CorrelationId = ev.CorrelationId ?? Guid.Empty,
        Payload = JsonSerializer.Serialize(ev)
      }).ToList();

      await _eventCollection.InsertManyAsync(storedEvents).ConfigureAwait(false);
      aggregate.MarkChangesAsCommitted();

      foreach (var ev in uncommitted)
        await _mediator.PublishEventAsync(ev).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes only events belonging to the current tenant and aggregate.
    /// </summary>
    public Task DeleteAsync(Guid id)
    {
      var tenantId = GetTenantIdOrThrow();
      var filter = TenantFilter(tenantId)
          & Builders<StoredEvent>.Filter.Eq(e => e.AggregateId, id);

      return _eventCollection.DeleteManyAsync(filter);
    }

    /// <summary>
    /// Creates tenant/aggregate replay and tenant/event identity indexes.
    /// Invoke from a deployment/index migration, not on every request.
    /// </summary>
    public async Task CreateTenantIndexesAsync(CancellationToken cancellationToken = default)
    {
      var replayKeys = Builders<StoredEvent>.IndexKeys
          .Ascending(e => e.TenantId)
          .Ascending(e => e.AggregateId)
          .Ascending(e => e.OccurredOn);
      var eventIdentityKeys = Builders<StoredEvent>.IndexKeys
          .Ascending(e => e.TenantId)
          .Ascending(e => e.EventId);

      await _eventCollection.Indexes.CreateManyAsync(
          new[]
          {
            new CreateIndexModel<StoredEvent>(
                replayKeys,
                new CreateIndexOptions { Name = "ix_tenant_aggregate_replay" }),
            new CreateIndexModel<StoredEvent>(
                eventIdentityKeys,
                new CreateIndexOptions { Name = "ux_tenant_event_id", Unique = true })
          },
          cancellationToken).ConfigureAwait(false);
    }

    private Guid? GetTenantIdOrThrow()
    {
      if (_tenantContextAccessor is null)
        return null;

      var tenantId = _tenantContextAccessor.Current?.TenantId;
      if (tenantId is null || tenantId == Guid.Empty)
        throw new TenantIsolationException(
            "MongoDB aggregate operations require a validated tenant execution context.");

      return tenantId.Value;
    }

    private static FilterDefinition<StoredEvent> TenantFilter(Guid? tenantId)
        => Builders<StoredEvent>.Filter.Eq(eventDocument => eventDocument.TenantId, tenantId);
  }

  /// <summary>
  /// Exception thrown when a requested aggregate is not found in the event store.
  /// </summary>
  public class NotFoundException : Exception
  {
    public NotFoundException(string message) : base(message) { }
  }
}
