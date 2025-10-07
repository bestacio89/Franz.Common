using Franz.Common.Errors;
using Franz.Common.Mapping.Abstractions;
using System.Collections;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Mapping.Core;

public class FranzMapper : IFranzMapper
{
  private readonly MappingConfiguration _config;

  public FranzMapper(MappingConfiguration config)
  {
    _config = config ?? throw new ArgumentNullException(nameof(config));
  }

  [return: NotNull]
  public TDestination Map<TSource, TDestination>([DisallowNull] TSource source)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    // Handle collection interface mappings dynamically
    if (IsCollectionInterface(typeof(TDestination)))
    {
      var mapped = MapCollection(typeof(TSource), typeof(TDestination), source);
      if (mapped is null)
      {
        throw new TechnicalException(
            $"Failed to map collection from {typeof(TSource).Name} to {typeof(TDestination).Name}.");
      }

      return (TDestination)mapped;
    }

    // Handle explicitly configured mappings
    if (_config.TryGetMapping<TSource, TDestination>(out var expression))
    {
      if (expression == null)
      {
        throw new TechnicalException(
            $"Mapping expression for {typeof(TSource).Name} to {typeof(TDestination).Name} is null.");
      }

      TDestination destination;
      if (expression.Constructor != null)
      {
        var constructed = expression.Constructor(source);
        destination = constructed ?? throw new TechnicalException(
            $"The constructor delegate for {typeof(TDestination).Name} returned null.");
      }
      else
      {
        destination = Activator.CreateInstance<TDestination>()
            ?? throw new TechnicalException(
                $"Could not create instance of {typeof(TDestination).Name}. " +
                "Make sure it has a parameterless constructor or use ConstructUsing().");
      }

      ApplyMapping(source, destination, expression);
      return destination;
    }

    // Fallback default mapping
    var fallback = DefaultMap<TSource, TDestination>(source);
    if (fallback == null)
    {
      throw new TechnicalException(
          $"Default mapping from {typeof(TSource).Name} to {typeof(TDestination).Name} failed. " +
          "Ensure both types have compatible public properties.");
    }

    return fallback;
  }


  private void ApplyMapping<TSource, TDestination>(
    [DisallowNull] TSource source,
    [DisallowNull] TDestination destination,
    [DisallowNull] MappingExpression<TSource, TDestination> expression)
  {
    if (source == null || destination == null || expression == null) return;

    var srcType = typeof(TSource);
    var destType = typeof(TDestination);

    foreach (var destProp in destType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
    {
      if (expression.IgnoredMembers.Contains(destProp.Name))
        continue;

      // Figure out which source name to use
      var srcName = expression.MemberBindings.TryGetValue(destProp.Name, out var boundName)
          ? boundName
          : destProp.Name;

      var srcProp = srcType
          .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
          .FirstOrDefault(p => p.Name.Equals(srcName, StringComparison.OrdinalIgnoreCase));

      if (srcProp == null) continue;

      var srcValue = srcProp.GetValue(source);

      if (srcValue == null)
      {
        if (!destProp.PropertyType.IsValueType || Nullable.GetUnderlyingType(destProp.PropertyType) != null)
          destProp.SetValue(destination, null);
        continue;
      }

      // Value object unwrapping (ex: ISBN.Value → string)
      var valueProp = srcValue.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
      if (valueProp != null && destProp.PropertyType.IsAssignableFrom(valueProp.PropertyType))
      {
        var unwrapped = valueProp.GetValue(srcValue);
        destProp.SetValue(destination, unwrapped);
        continue;
      }

      // Handle collections
      if (typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType) && destProp.PropertyType != typeof(string))
      {
        var mappedCollection = MapCollection(srcProp.PropertyType, destProp.PropertyType, srcValue);
        destProp.SetValue(destination, mappedCollection);
        continue;
      }

      // Handle nested objects recursively
      if (!destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
      {
        var mapMethod = typeof(FranzMapper)
            .GetMethod(nameof(Map), BindingFlags.Instance | BindingFlags.Public)?
            .MakeGenericMethod(srcProp.PropertyType, destProp.PropertyType);

        if (mapMethod != null)
        {
          var nested = mapMethod.Invoke(this, new[] { srcValue });
          destProp.SetValue(destination, nested);
        }
      }
      else
      {
        destProp.SetValue(destination, srcValue);
      }
    }
  }


  private static TDestination DefaultMap<TSource, TDestination>([DisallowNull] TSource source)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    var dest = Activator.CreateInstance<TDestination>()
        ?? throw new TechnicalException($"Failed to instantiate {typeof(TDestination).Name}.");

    foreach (var prop in typeof(TDestination).GetProperties().Where(p => p.CanWrite))
    {
      var sourceProp = typeof(TSource).GetProperty(prop.Name);
      if (sourceProp != null)
        prop.SetValue(dest, sourceProp.GetValue(source));
    }

    return dest;
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
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    var srcElemType = GetElementType(sourceType);
    var destElemType = GetElementType(destType);

    var listType = typeof(List<>).MakeGenericType(destElemType);
    var result = (IList)(Activator.CreateInstance(listType)
        ?? throw new TechnicalException($"Could not instantiate List<{destElemType.Name}>."));

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


  private static Type GetElementType(Type seqType)
      => seqType.IsArray ? seqType.GetElementType()! :
         seqType.IsGenericType ? seqType.GetGenericArguments()[0] :
         typeof(object);
}
