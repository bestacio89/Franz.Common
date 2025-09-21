using Franz.Common.Aras.Mappings.Contracts;
using Franz.Common.Aras.Mappings.Contracts.Mappers;
using Franz.Common.Business;
using Franz.Common.Business.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Aras.Mappings.Factories;

/// <summary>
/// Factory to resolve entity mappers via DI.
/// </summary>
public interface IArasEntityMapperFactory
{
  IArasEntityMapper<TEntity> Resolve<TEntity>()
      where TEntity : Entity<Guid>;
}




