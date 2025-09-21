using System;
using System.Collections.Generic;

namespace Franz.Common.Aras.Abstractions.Mappings.Implementations.Mappers
{
  /// <summary>
  /// Registry for all ARAS mapping configurations.
  /// Similar to EF's ModelBuilder/AutoMapper profiles.
  /// </summary>
  public class ArasMappingRegistry
  {
    private readonly Dictionary<Type, object> _entityMaps = new();

    public void Register<TEntity>(ArasEntityMap<TEntity> map)
    {
      _entityMaps[typeof(TEntity)] = map ?? throw new ArgumentNullException(nameof(map));
    }

    public ArasEntityMap<TEntity>? Resolve<TEntity>()
    {
      if (_entityMaps.TryGetValue(typeof(TEntity), out var map))
      {
        return (ArasEntityMap<TEntity>)map;
      }
      return null;
    }
  }
}
