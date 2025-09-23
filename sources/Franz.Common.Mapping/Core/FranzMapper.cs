using System.Collections;
using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.Mapping.Core;

public class FranzMapper : IFranzMapper
{
  private readonly MappingConfiguration _config;

  public FranzMapper(MappingConfiguration config) => _config = config;

  public TDestination Map<TSource, TDestination>(TSource source)
  {
    if (source == null) throw new ArgumentNullException(nameof(source));

    // Handle collection interface mappings dynamically
    if (IsCollectionInterface(typeof(TDestination)))
    {
      return (TDestination)MapCollection(typeof(TSource), typeof(TDestination), source!);
    }

    // Handle explicitly configured mappings
    if (_config.TryGetMapping<TSource, TDestination>(out var expression))
      return ApplyMapping(source, expression!);

    // Fallback default mapping
    return DefaultMap<TSource, TDestination>(source);
  }

  private static TDestination ApplyMapping<TSource, TDestination>(
      TSource source,
      MappingExpression<TSource, TDestination> expression)
  {
    // Construct destination
    var dest = expression.Constructor != null
        ? expression.Constructor(source)
        : Activator.CreateInstance<TDestination>();

    foreach (var prop in typeof(TDestination).GetProperties().Where(p => p.CanWrite))
    {
      if (expression.Ignored.Contains(prop.Name))
        continue;

      if (expression.MemberMap.TryGetValue(prop.Name, out var sourceName))
      {
        // Look up the source property by name
        var sourceProp = typeof(TSource).GetProperty(sourceName);
        if (sourceProp != null)
        {
          var value = sourceProp.GetValue(source);
          prop.SetValue(dest, value);
        }
      }
      else
      {
        // Fallback: same-name mapping
        var sourceProp = typeof(TSource).GetProperty(prop.Name);
        if (sourceProp != null)
        {
          var value = sourceProp.GetValue(source);
          prop.SetValue(dest, value);
        }
      }
    }

    return dest!;
  }

  private static TDestination DefaultMap<TSource, TDestination>(TSource source)
  {
    var dest = Activator.CreateInstance<TDestination>();

    foreach (var prop in typeof(TDestination).GetProperties().Where(p => p.CanWrite))
    {
      var sourceProp = typeof(TSource).GetProperty(prop.Name);
      if (sourceProp != null)
        prop.SetValue(dest, sourceProp.GetValue(source));
    }

    return dest!;
  }

  private static bool IsCollectionInterface(Type type)
  {
    if (!type.IsGenericType) return false;

    var def = type.GetGenericTypeDefinition();
    return def == typeof(IEnumerable<>) ||
           def == typeof(ICollection<>) ||
           def == typeof(IReadOnlyCollection<>) ||
           def == typeof(IList<>);
  }

  private object MapCollection(Type sourceType, Type destType, object source)
  {
    var sourceEnumerable = source as IEnumerable;
    if (sourceEnumerable == null)
      throw new InvalidOperationException($"Source {sourceType} is not enumerable");

    var elementType = destType.GetGenericArguments()[0];
    var listType = typeof(List<>).MakeGenericType(elementType);
    var list = (IList)Activator.CreateInstance(listType)!;

    foreach (var item in sourceEnumerable)
    {
      // Call Map<TSourceElement, TDestElement>
      var mappedItem = typeof(FranzMapper)
          .GetMethod(nameof(Map), new[] { item.GetType() })!
          .MakeGenericMethod(item.GetType(), elementType)
          .Invoke(this, new[] { item });

      list.Add(mappedItem!);
    }

    // If target type is interface, return as is
    if (IsCollectionInterface(destType))
      return list;

    // Otherwise cast to requested concrete type
    return Convert.ChangeType(list, destType);
  }
}
