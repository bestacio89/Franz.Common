using System;
using System.Collections.Generic;

namespace Franz.Common.Aras.Abstractions.Mappings.Implementations.Mappers
{
  /// <summary>
  /// Fluent API configuration for ARAS → Entity mappings.
  /// </summary>
  public class ArasEntityMap<TEntity>
  {
    private readonly Dictionary<string, Action<TEntity, object>> _propertySetters = new();
    private readonly Dictionary<string, Func<TEntity, object>> _propertyGetters = new();
    private readonly HashSet<string> _ignoredProperties = new();

    public ArasEntityMap<TEntity> MapProperty<TProperty>(
        string arasField,
        Func<TEntity, TProperty> getter,
        Action<TEntity, TProperty> setter)
    {
      _propertyGetters[arasField] = e => getter(e)!;
      _propertySetters[arasField] = (e, v) => setter(e, (TProperty)Convert.ChangeType(v, typeof(TProperty)));
      return this;
    }

    public ArasEntityMap<TEntity> IgnoreProperty(string arasField)
    {
      _ignoredProperties.Add(arasField);
      return this;
    }

    public TEntity ApplyFromAras(IDictionary<string, object> arasData, TEntity entity)
    {
      foreach (var kvp in arasData)
      {
        if (_ignoredProperties.Contains(kvp.Key)) continue;

        if (_propertySetters.TryGetValue(kvp.Key, out var setter))
        {
          setter(entity, kvp.Value);
        }
      }
      return entity;
    }

    public IDictionary<string, object> ExtractToAras(TEntity entity)
    {
      var result = new Dictionary<string, object>();

      foreach (var kvp in _propertyGetters)
      {
        result[kvp.Key] = kvp.Value(entity);
      }

      return result;
    }
  }
}
