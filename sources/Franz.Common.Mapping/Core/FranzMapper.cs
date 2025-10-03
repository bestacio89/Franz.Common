using Franz.Common.Errors;
using Franz.Common.Mapping.Abstractions;
using System.Collections;
using System.Reflection;

namespace Franz.Common.Mapping.Core;

public class FranzMapper : IFranzMapper
{
  private readonly MappingConfiguration _config;

  public FranzMapper(MappingConfiguration config) => _config = config;

  public TDestination Map<TSource, TDestination>(TSource source)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    // Handle collection interface mappings dynamically
    if (IsCollectionInterface(typeof(TDestination)))
    {
      return (TDestination)MapCollection(typeof(TSource), typeof(TDestination), source)!;
    }

    // Handle explicitly configured mappings
    if (_config.TryGetMapping<TSource, TDestination>(out var expression))
    {
      TDestination? destination;
      if (expression.Constructor != null)
        destination = expression.Constructor(source);
      else
      {
        destination = Activator.CreateInstance<TDestination>();
      }

      if (destination is null)
      {
        throw new TechnicalException(
            $"Could not create instance of {typeof(TDestination).Name}. " +
            "Make sure it has a parameterless constructor or use ConstructUsing().");
      }

      // Apply mapping rules safely
      ApplyMapping(source, destination, expression);

      return destination;
    }

    // Fallback default mapping
    return DefaultMap<TSource, TDestination>(source);
  }




  private void ApplyMapping<TSource, TDestination>(
    TSource source,
    TDestination destination,
    MappingExpression<TSource, TDestination> expression)
{
    if (source == null || destination == null) return;

    var srcType = typeof(TSource);
    var destType = typeof(TDestination);

    foreach (var destProp in destType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
    {
        // Skip ignored members
        if (expression.IgnoredMembers.Contains(destProp.Name))
            continue;

        // Figure out which source name to use (mapping or same name)
        string srcName = expression.MemberBindings.TryGetValue(destProp.Name, out var boundName)
            ? boundName
            : destProp.Name;

        // Safe property resolution on the source type
        var srcProps = srcType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            .Where(p => p.Name.Equals(srcName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (srcProps.Length != 1)
        {
            // Ambiguous or missing → skip this property
            continue;
        }

        var srcProp = srcProps[0];
        var srcValue = srcProp.GetValue(source);

        if (srcValue == null)
        {
            // Assign null if destination is nullable
            if (!destProp.PropertyType.IsValueType || Nullable.GetUnderlyingType(destProp.PropertyType) != null)
                destProp.SetValue(destination, null);
            continue;
        }

        // Handle value object unwrapping (ex: ISBN.Value → string)
        var valueProp = srcValue.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        if (valueProp != null && destProp.PropertyType.IsAssignableFrom(valueProp.PropertyType))
        {
            var unwrapped = valueProp.GetValue(srcValue);
            destProp.SetValue(destination, unwrapped);
            continue;
        }

        // Handle collections
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(destProp.PropertyType) &&
            destProp.PropertyType != typeof(string))
        {
            var mappedCollection = MapCollection(srcProp.PropertyType, destProp.PropertyType, srcValue);
            destProp.SetValue(destination, mappedCollection);
            continue;
        }

        // Handle nested objects with recursive mapping
        if (!destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
        {
            var mapMethod = typeof(FranzMapper)
                .GetMethod(nameof(Map), BindingFlags.Instance | BindingFlags.Public)
                ?.MakeGenericMethod(srcProp.PropertyType, destProp.PropertyType);

            if (mapMethod != null)
            {
                var nested = mapMethod.Invoke(this, new[] { srcValue });
                destProp.SetValue(destination, nested);
            }
        }
        else
        {
            // Direct assignment
            destProp.SetValue(destination, srcValue);
        }
    }
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
    if (source == null) return new { };

    var srcElemType = GetElementType(sourceType);
    var destElemType = GetElementType(destType);

    var listType = typeof(List<>).MakeGenericType(destElemType);
    var result = (IList)Activator.CreateInstance(listType)!;

    var mapMethod = typeof(FranzMapper)
        .GetMethod(nameof(Map), BindingFlags.Instance | BindingFlags.Public)!
        .MakeGenericMethod(srcElemType, destElemType);

    foreach (var item in (IEnumerable)source)
    {
      if (item == null)
      {
        result.Add(null);
        continue;
      }

      try
      {
        var mapped = mapMethod.Invoke(this, new[] { item });
        result.Add(mapped);
      }
      catch (TargetInvocationException ex)
      {
        // rethrow the actual inner error instead of the wrapper
        throw ex.InnerException ?? ex;
      }
    }

    if (destType.IsArray)
    {
      var arr = Array.CreateInstance(destElemType, result.Count);
      result.CopyTo(arr, 0);
      return arr;
    }

    return result;
  }

  private static Type GetElementType(Type seqType) =>
      seqType.IsArray ? seqType.GetElementType()! :
      seqType.IsGenericType ? seqType.GetGenericArguments()[0] :
      typeof(object);






}
