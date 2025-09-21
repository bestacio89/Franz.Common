using Franz.Common.Aras.Mappings.Contracts.Factories;
using Franz.Common.Aras.Mappings.Contracts.Mappers;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Mappings.Implementations.Factories;
public class ArasAggregateMapperFactory : IArasAggregateMapperFactory
{
  private readonly IServiceProvider _provider;

  public ArasAggregateMapperFactory(IServiceProvider provider) => _provider = provider;

  public IArasAggregateMapper<TAggregate, TEvent> Resolve<TAggregate, TEvent>()
      where TAggregate : AggregateRoot<TEvent>
      where TEvent : BaseDomainEvent
      => _provider.GetRequiredService<IArasAggregateMapper<TAggregate, TEvent>>();
}
