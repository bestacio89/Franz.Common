using Franz.Common.Aras.Mappings.Contracts.Mappers;
using System.Reflection;

namespace Franz.Common.Aras.Mappings.Implementations.Mappers;
public class DefaultArasEntityMapper<TEntity> : IArasEntityMapper<TEntity>
       where TEntity : new()
{
  public TEntity MapFromAras(IDictionary<string, object> arasData)
  {
    var entity = new TEntity();
    var props = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var prop in props)
    {
      if (arasData.TryGetValue(prop.Name, out var value) && value != null)
        prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
    }

    return entity;
  }

  public IDictionary<string, object> MapToAras(TEntity entity)
  {
    var dict = new Dictionary<string, object>();
    var props = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var prop in props)
    {
      var value = prop.GetValue(entity);
      if (value != null)
        dict[prop.Name] = value;
    }

    return dict;
  }
}