using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.Mapping.Core;

public class FranzMapper : IFranzMapper
{
  private readonly MappingConfiguration _config;

  public FranzMapper(MappingConfiguration config) => _config = config;

  public TDestination Map<TSource, TDestination>(TSource source)
  {
    if (source == null) throw new ArgumentNullException(nameof(source));

    if (_config.TryGetMapping<TSource, TDestination>(out var expression))
      return ApplyMapping(source, expression!);

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
}
