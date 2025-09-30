using Franz.Common.Aras.Mappings.Contracts.Mappers;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;

namespace Franz.Common.Aras.Mappings.Contracts.Factories
{
  /// <summary>
  /// Factory to resolve aggregate mappers via dependency injection.
  /// Provides the correct <see cref="IArasAggregateMapper{TAggregate,TDomainEvent}"/>
  /// for the given aggregate and its domain event type.
  /// </summary>
  public interface IArasAggregateMapperFactory
  {
    IArasAggregateMapper<TAggregate, TDomainEvent> Resolve<TAggregate, TDomainEvent>()
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent;
  }
}
