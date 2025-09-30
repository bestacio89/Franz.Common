using Franz.Common.Aras.Mappings.Contracts.Mappers;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

namespace Franz.Common.Aras.Mappings.Implementations.Mappers
{
  /// <summary>
  /// Default mapper implementation for ARAS aggregates.
  /// Uses an <see cref="IArasEntityMapper{TEntity}"/> to translate
  /// between ARAS state (dictionary fields) and aggregate instances.
  /// Also supports event-based reconstruction of aggregates.
  /// </summary>
  public sealed class DefaultArasAggregateMapper<TAggregate, TDomainEvent>
      : IArasAggregateMapper<TAggregate, TDomainEvent>
      where TAggregate : AggregateRoot<TDomainEvent>, new()
      where TDomainEvent : IDomainEvent
  {
    private readonly IArasEntityMapper<TAggregate> _entityMapper;

    public DefaultArasAggregateMapper(IArasEntityMapper<TAggregate> entityMapper)
    {
      _entityMapper = entityMapper;
    }

    /// <summary>
    /// Hydrates an aggregate from ARAS field data.
    /// </summary>
    public TAggregate MapFromState(IDictionary<string, object> arasData)
    {
      return _entityMapper.MapFromAras(arasData);
    }

    /// <summary>
    /// Reconstructs an aggregate by replaying its domain events.
    /// </summary>
    public TAggregate MapFromEvents(IEnumerable<TDomainEvent> events)
    {
      var aggregate = new TAggregate();
      aggregate.ReplayEvents(events);
      return aggregate;
    }

    /// <summary>
    /// Maps the current aggregate state to ARAS field data.
    /// </summary>
    public IDictionary<string, object> MapToAras(TAggregate aggregate)
    {
      return _entityMapper.MapToAras(aggregate);
    }
  }
}
