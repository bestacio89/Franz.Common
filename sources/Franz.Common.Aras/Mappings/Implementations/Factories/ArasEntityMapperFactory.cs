using Franz.Common.Aras.Mappings.Contracts.Mappers;
using Franz.Common.Aras.Mappings.Factories;
using Franz.Common.Business.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Mappings.Implementations.Factories;
public class ArasEntityMapperFactory : IArasEntityMapperFactory
{
  private readonly IServiceProvider _provider;

  public ArasEntityMapperFactory(IServiceProvider provider) => _provider = provider;

  public IArasEntityMapper<TEntity> Resolve<TEntity>()
      where TEntity : Entity<Guid>
      => _provider.GetRequiredService<IArasEntityMapper<TEntity>>();
}
