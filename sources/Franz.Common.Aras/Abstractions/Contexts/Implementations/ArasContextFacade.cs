using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Dispatchers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Persistence
{
  /// <summary>
  /// Developer-friendly façade that unifies entity and aggregate contexts.
  /// Provides clean syntax sugar via Entities and Aggregates properties.
  /// </summary>
  public sealed class ArasContextFacade : IDisposable
  {
    private readonly IArasEntityContext _entityCtx;
    private readonly IArasAggregateContext _aggCtx;

    public ArasContextFacade(
        IArasEntityContext entityCtx,
        IArasAggregateContext aggCtx)
    {
      _entityCtx = entityCtx ?? throw new ArgumentNullException(nameof(entityCtx));
      _aggCtx = aggCtx ?? throw new ArgumentNullException(nameof(aggCtx));

      Entities = new EntityOps(_entityCtx);
      Aggregates = new AggregateOps(_aggCtx);
    }

    /// <summary>
    /// Exposes dispatcher from the aggregate context
    /// so domain events can flow into Franz pipelines.
    /// </summary>
    public IDispatcher Dispatcher => _aggCtx.Dispatcher;

    /// <summary>
    /// Developer-friendly entry point for CRUD-style operations on entities.
    /// </summary>
    public EntityOps Entities { get; }

    /// <summary>
    /// Developer-friendly entry point for DDD-style operations on aggregates.
    /// </summary>
    public AggregateOps Aggregates { get; }

    // ---------------- ENTITY OPS ----------------
    public sealed class EntityOps
    {
      private readonly IArasEntityContext _ctx;
      internal EntityOps(IArasEntityContext ctx) => _ctx = ctx;

      public Task<IReadOnlyCollection<TEntity>> Query<TEntity>(
          string query,
          CancellationToken ct = default)
        where TEntity : Entity<Guid>
        => _ctx.QueryEntitiesAsync<TEntity>(query, ct);

      public Task<TEntity?> ById<TEntity>(
          Guid id,
          CancellationToken ct = default)
        where TEntity : Entity<Guid>
        => _ctx.GetEntityByIdAsync<TEntity>(id, ct);

      public Task Save<TEntity>(
          TEntity entity,
          CancellationToken ct = default)
        where TEntity : Entity<Guid>
        => _ctx.SaveEntityAsync(entity, ct);

      public Task Delete<TEntity>(
          Guid id,
          CancellationToken ct = default)
        where TEntity : Entity<Guid>
        => _ctx.DeleteEntityAsync<TEntity>(id, ct);
    }

    // ---------------- AGGREGATE OPS ----------------
    public sealed class AggregateOps
    {
      private readonly IArasAggregateContext _ctx;
      internal AggregateOps(IArasAggregateContext ctx) => _ctx = ctx;

      public Task<TAggregate?> ById<TAggregate, TEvent>(
          Guid id,
          CancellationToken ct = default)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent
        => _ctx.GetAggregateAsync<TAggregate, TEvent>(id, ct);

      public Task Save<TAggregate, TEvent>(
          TAggregate aggregate,
          CancellationToken ct = default)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent
        => _ctx.SaveAggregateAsync<TAggregate, TEvent>(aggregate, ct);

      public void Track<TAggregate, TEvent>(
          TAggregate aggregate)
        where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
        where TEvent : BaseDomainEvent
        => _ctx.TrackAggregate<TAggregate, TEvent>(aggregate);

      public Task<int> Commit(CancellationToken ct = default)
        => _ctx.SaveAggregateChangesAsync(ct);
    }

    public void Dispose()
    {
      (_entityCtx as IDisposable)?.Dispose();
      (_aggCtx as IDisposable)?.Dispose();
    }
  }
}
