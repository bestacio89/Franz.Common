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

  private object MapCollection(Type sourceType, Type destType, object? source)
  {
    // 1. Null-safe
    if (source is null)
    {
      var elem = destType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
      if (destType.IsArray) return Array.CreateInstance(elem, 0);
      return Activator.CreateInstance(typeof(List<>).MakeGenericType(elem))!;
    }

    // 2. Ensure it's enumerable
    if (source is not IEnumerable srcEnum)
      throw new InvalidOperationException($"Source {sourceType} is not enumerable");

    // 3. Work out element types
    var destElemType = destType.IsArray
        ? destType.GetElementType()!
        : destType.GetGenericArguments()[0];

    // 4. Prepare a working List<destElemType>
    var listType = typeof(List<>).MakeGenericType(destElemType);
    var list = (IList)Activator.CreateInstance(listType)!;

    // 5. Get open generic Map<TSource,TDest>
    var mapMethod = typeof(FranzMapper)
        .GetMethods()
        .First(m => m.Name == nameof(Map) && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2);

    foreach (var item in srcEnum)
    {
      if (item is null) { list.Add(default!); continue; }

      var concrete = mapMethod.MakeGenericMethod(item.GetType(), destElemType);
      var mapped = concrete.Invoke(this, new[] { item });
      list.Add(mapped!);
    }

    // 6. Shape output
    if (destType.IsArray)
    {
      var arr = Array.CreateInstance(destElemType, list.Count);
      list.CopyTo(arr, 0);
      return arr;
    }

    if (destType.IsInterface)
      return list; // e.g. IEnumerable<T>, IReadOnlyCollection<T>, IList<T>

    if (destType.IsAssignableFrom(listType))
      return list; // destination is List<T>

    // Last resort: try to build the concrete destination and populate
    var destCollection = Activator.CreateInstance(destType);
    if (destCollection is IList destList)
    {
      foreach (var obj in list) destList.Add(obj);
      return destList;
    }

    return list; // fallback
  }

}
