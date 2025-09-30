using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Franz.Common.Aras.Diagnostics
{
  /// <summary>
  /// Diagnostic decorator for <see cref="IArasAggregateContext"/>.
  /// Adds structured logging and OpenTelemetry tracing around aggregate operations.
  /// </summary>
  public sealed class DiagnosticAggregateContextDecorator : IArasAggregateContext
  {
    private readonly IArasAggregateContext _inner;
    private readonly ILogger<DiagnosticAggregateContextDecorator> _logger;
    private readonly ActivitySource _activitySource;

    public DiagnosticAggregateContextDecorator(
        IArasAggregateContext inner,
        ILogger<DiagnosticAggregateContextDecorator> logger,
        ActivitySource activitySource)
    {
      _inner = inner;
      _logger = logger;
      _activitySource = activitySource;
    }

   

    public void TrackAggregate<TAggregate, TDomainEvent>(TAggregate aggregate)
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      _logger.LogInformation(
          "Tracking aggregate {Aggregate} with Id {Id}",
          typeof(TAggregate).Name, aggregate.Id);

      _inner.TrackAggregate<TAggregate, TDomainEvent>(aggregate);
    }

    public async Task<int> SaveAggregateChangesAsync(CancellationToken ct = default)
    {
      using var activity = _activitySource.StartActivity("Aras.SaveAggregateChanges");

      _logger.LogInformation("Committing tracked aggregates to ARAS...");

      var count = await _inner.SaveAggregateChangesAsync(ct);

      _logger.LogInformation("Committed {Count} aggregates to ARAS", count);

      return count;
    }

    public async Task<TAggregate?> GetAggregateAsync<TAggregate, TDomainEvent>(
        Guid id, CancellationToken ct = default
    )
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      using var activity = _activitySource.StartActivity("Aras.GetAggregate");
      activity?.SetTag("aggregate", typeof(TAggregate).Name);
      activity?.SetTag("id", id);

      _logger.LogInformation(
          "Fetching ARAS aggregate {Aggregate} with Id {Id}",
          typeof(TAggregate).Name, id);

      var result = await _inner.GetAggregateAsync<TAggregate, TDomainEvent>(id, ct);

      _logger.LogInformation(
          "Fetched ARAS aggregate {Aggregate} with Id {Id}: Found={Found}",
          typeof(TAggregate).Name, id, result != null);

      return result;
    }

    public async Task SaveAggregateAsync<TAggregate, TDomainEvent>(
        TAggregate aggregate,
        CancellationToken ct = default
    )
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      using var activity = _activitySource.StartActivity("Aras.SaveAggregate");
      activity?.SetTag("aggregate", typeof(TAggregate).Name);
      activity?.SetTag("id", aggregate.Id);

      _logger.LogInformation(
          "Saving ARAS aggregate {Aggregate} with Id {Id}",
          typeof(TAggregate).Name, aggregate.Id);

      await _inner.SaveAggregateAsync<TAggregate, TDomainEvent>(aggregate, ct);

      _logger.LogInformation(
          "Saved ARAS aggregate {Aggregate} with Id {Id}",
          typeof(TAggregate).Name, aggregate.Id);
    }

    public IAggregateSnapshotStore<TAggregate, TDomainEvent> SnapshotStore<TAggregate, TDomainEvent>()
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      _logger.LogInformation("Resolving SnapshotStore for {Aggregate}", typeof(TAggregate).Name);
      return _inner.SnapshotStore<TAggregate, TDomainEvent>();
    }

  
  }
}
