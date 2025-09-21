using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Abstractions.Snapshots.Contracts;
using Franz.Common.Aras.Mappings.Contracts.Factories;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Franz.Common.Mediator.Dispatchers;
using System.Net.Http.Json;
using System.Reflection;

namespace Franz.Common.Aras.Innovator.Contexts;

public class ArasInnovatorAggregateContext : IArasAggregateContext
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

  public void TrackAggregate<TAggregate, TEvent>(TAggregate aggregate)
      where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
      where TEvent : BaseDomainEvent
  {
    _trackedAggregates.Add((aggregate, typeof(TAggregate), typeof(TEvent)));
  }

  public async Task<int> SaveAggregateChangesAsync(CancellationToken ct = default)
  {
    foreach (var (aggregate, aggType, evtType) in _trackedAggregates)
    {
      // Call PersistAggregateAsync<TAggregate,TEvent> dynamically
      var method = typeof(ArasInnovatorAggregateContext)
          .GetMethod(nameof(PersistAggregateAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
          .MakeGenericMethod(aggType, evtType);

      await (Task)method.Invoke(this, new object[] { aggregate, ct })!;

      var root = (IAggregateRoot)aggregate;

      foreach (var ev in root.GetUncommittedChanges())
        await _dispatcher.PublishAsync(ev, ct);

      root.MarkChangesAsCommitted();
    }

    var count = _trackedAggregates.Count;
    _trackedAggregates.Clear();
    return count;
  }

  public async Task<TAggregate?> GetAggregateAsync<TAggregate, TEvent>(
      Guid id,
      CancellationToken ct = default
  )
      where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
      where TEvent : BaseDomainEvent
  {
    var response = await _client.GetAsync($"/api/v1/{typeof(TAggregate).Name}/{id}", ct);
    if (!response.IsSuccessStatusCode) return null;

    var arasData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ct);
    if (arasData is null) return null;

    var mapper = _mapperFactory.Resolve<TAggregate, TEvent>();
    return mapper.MapFromState(arasData);
  }

  public async Task SaveAggregateAsync<TAggregate, TEvent>(
      TAggregate aggregate,
      CancellationToken ct = default
  )
      where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
      where TEvent : BaseDomainEvent
  {
    await PersistAggregateAsync<TAggregate, TEvent>(aggregate, ct);

    var lastEvent = aggregate.GetUncommittedChanges().LastOrDefault();
    if (lastEvent != null)
      await _dispatcher.PublishAsync(lastEvent, ct);

    aggregate.MarkChangesAsCommitted();
  }

  // Strongly typed persistence logic
  private async Task PersistAggregateAsync<TAggregate, TEvent>(
    TAggregate aggregate,
    CancellationToken ct
)
    where TAggregate : AggregateRoot<TEvent>, IAggregateRoot, new()
    where TEvent : BaseDomainEvent
  {
    var mapper = _mapperFactory.Resolve<TAggregate, TEvent>();
    var arasData = mapper.MapToAras(aggregate);

    HttpResponseMessage response;
    if (aggregate.Id == Guid.Empty)
      response = await _client.PostAsJsonAsync($"/api/v1/{typeof(TAggregate).Name}", arasData, ct);
    else
      response = await _client.PutAsJsonAsync($"/api/v1/{typeof(TAggregate).Name}/{aggregate.Id}", arasData, ct);

    response.EnsureSuccessStatusCode();
  }

  public IAggregateSnapshotStore<TAggregate, TEvent> SnapshotStore<TAggregate, TEvent>()
      where TAggregate : AggregateRoot<TEvent>, new()
      where TEvent : BaseDomainEvent
  {
    // Plug your snapshot store here when ready
    throw new NotImplementedException();
  }

  public void Dispose() => _client.Dispose();
}
