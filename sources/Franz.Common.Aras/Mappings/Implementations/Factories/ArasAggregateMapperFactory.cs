using Franz.Common.Aras.Mappings.Contracts.Factories;
using Franz.Common.Aras.Mappings.Contracts.Mappers;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Aras.Mappings.Implementations.Factories
{
  /// <summary>
  /// Default implementation of <see cref="IArasAggregateMapperFactory"/>.
  /// Resolves aggregate mappers from the DI container.
  /// </summary>
  public sealed class ArasAggregateMapperFactory : IArasAggregateMapperFactory
  {
    private readonly IServiceProvider _provider;

    public ArasAggregateMapperFactory(IServiceProvider provider)
      => _provider = provider;

    public IArasAggregateMapper<TAggregate, TDomainEvent> Resolve<TAggregate, TDomainEvent>()
        where TAggregate : AggregateRoot<TDomainEvent>, new()
        where TDomainEvent : IDomainEvent
    {
      try
      {
        return _provider.GetRequiredService<IArasAggregateMapper<TAggregate, TDomainEvent>>();
      }
      catch (InvalidOperationException ex)
      {
        throw new InvalidOperationException(
          $"No mapper registered for aggregate '{typeof(TAggregate).Name}' " +
          $"with event type '{typeof(TDomainEvent).Name}'. " +
          $"Ensure you have registered IArasAggregateMapper<{typeof(TAggregate).Name}, {typeof(TDomainEvent).Name}> " +
          $"in your DI container.", ex);
      }
    }
  }
}
