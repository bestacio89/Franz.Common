using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Aras.Mappings.Contracts.Factories;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator.Dispatchers;
using System.Net.Http.Json;
using System.Reflection;

namespace Franz.Common.Aras.Innovator.Contexts;

/// <summary>
/// ARAS-specific aggregate context implementation that
/// persists aggregates over HTTP and dispatches domain events
/// into Franz pipelines.
/// </summary>
public sealed class ArasInnovatorAggregateContext : IArasAggregateContext
{
  private readonly HttpClient _client;
  private readonly IDispatcher _dispatcher;
  private readonly IArasAggregateMapperFactory _mapperFactory;

  // Track aggregates along with their concrete type + event type
  private readonly List<(object Aggregate, Type AggregateType, Type EventType)> _trackedAggregates = new();

  public ArasInnovatorAggregateContext(
      HttpClient client,
      IDispatcher dispatcher,
      IArasAggregateMapperFactory mapperFactory)
  {
    _client = client;
    _dispatcher = dispatcher;
    _mapperFactory = mapperFactory;
  }

  public IDispatcher Dispatcher => _dispatcher;

  public void TrackAggregate<TAggregate, TDomainEvent>(TAggregate aggregate)
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent
  {
    _trackedAggregates.Add((aggregate, typeof(TAggregate), typeof(TDomainEvent)));
  }

  public async Task<int> SaveAggregateChangesAsync(CancellationToken ct = default)
  {
    foreach (var (aggregate, aggType, evtType) in _trackedAggregates)
    {
      // Call PersistAggregateAsync<TAggregate,TDomainEvent> dynamically
      var method = typeof(ArasInnovatorAggregateContext)
          .GetMethod(nameof(PersistAggregateAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
          .MakeGenericMethod(aggType, evtType);

      await (Task)method.Invoke(this, new object[] { aggregate, ct })!;

      // Dispatch all uncommitted domain events
      var root = (AggregateRoot<IDomainEvent>)aggregate;
      var changes = root.GetUncommittedChanges().ToList();

      foreach (var ev in changes)
        await _dispatcher.PublishEventAsync(ev, ct);

      root.MarkChangesAsCommitted();
    }

    var count = _trackedAggregates.Count;
    _trackedAggregates.Clear();
    return count;
  }

  public async Task<TAggregate?> GetAggregateAsync<TAggregate, TDomainEvent>(
      Guid id,
      CancellationToken ct = default
  )
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent
  {
    var response = await _client.GetAsync($"/api/v1/{typeof(TAggregate).Name}/{id}", ct);
    if (!response.IsSuccessStatusCode) return null;

    var arasData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ct);
    if (arasData is null) return null;

    var mapper = _mapperFactory.Resolve<TAggregate, TDomainEvent>();
    return mapper.MapFromState(arasData);
  }

  public async Task SaveAggregateAsync<TAggregate, TDomainEvent>(
      TAggregate aggregate,
      CancellationToken ct = default
  )
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent
  {
    await PersistAggregateAsync<TAggregate, TDomainEvent>(aggregate, ct);

    var changes = aggregate.GetUncommittedChanges().ToList();
    foreach (var ev in changes)
      await _dispatcher.PublishEventAsync(ev, ct);

    aggregate.MarkChangesAsCommitted();
  }

  // Strongly typed persistence logic
  private async Task PersistAggregateAsync<TAggregate, TDomainEvent>(
      TAggregate aggregate,
      CancellationToken ct
  )
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent
  {
    var mapper = _mapperFactory.Resolve<TAggregate, TDomainEvent>();
    var arasData = mapper.MapToAras(aggregate);

    HttpResponseMessage response;
    if (aggregate.Id == Guid.Empty)
      response = await _client.PostAsJsonAsync($"/api/v1/{typeof(TAggregate).Name}", arasData, ct);
    else
      response = await _client.PutAsJsonAsync($"/api/v1/{typeof(TAggregate).Name}/{aggregate.Id}", arasData, ct);

    response.EnsureSuccessStatusCode();
  }

  public IAggregateSnapshotStore<TAggregate, TDomainEvent> SnapshotStore<TAggregate, TDomainEvent>()
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent
  {
    // Plug your snapshot store here when ready
    throw new NotImplementedException();
  }

  public void Dispose() => _client.Dispose();
}
