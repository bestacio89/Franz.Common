using Franz.Common.Mapping.Abstractions;
using System.Linq.Expressions;

namespace Franz.Common.Mapping.Core
{
  public class FranzMapper : IFranzMapper
  {
    private readonly MappingConfiguration _config;

    public FranzMapper(MappingConfiguration config)
    {
      _config = config;
    }

    public TDestination Map<TSource, TDestination>(TSource source)
    {
      if (source == null) throw new ArgumentNullException(nameof(source));

      if (_config.TryGetMapping<TSource, TDestination>(out var expression))
      {
        return ApplyMapping(source, expression!);
      }

      // Default by-name mapping
      return DefaultMap<TSource, TDestination>(source);
    }

    private TDestination ApplyMapping<TSource, TDestination>(
        TSource source,
        MappingExpression<TSource, TDestination> expression)
    {
      var dest = Activator.CreateInstance<TDestination>();

      foreach (var prop in typeof(TDestination).GetProperties().Where(p => p.CanWrite))
      {
        if (expression.Ignored.Contains(prop.Name))
          continue;

        string sourceName = expression.MemberMap.TryGetValue(prop.Name, out var mapped) ? mapped : prop.Name;

        var sourceProp = typeof(TSource).GetProperty(sourceName);
        if (sourceProp != null)
        {
          var value = sourceProp.GetValue(source);
          prop.SetValue(dest, value);
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
        {
          var value = sourceProp.GetValue(source);
          prop.SetValue(dest, value);
        }
      }

      return dest!;
    }
  }
}
