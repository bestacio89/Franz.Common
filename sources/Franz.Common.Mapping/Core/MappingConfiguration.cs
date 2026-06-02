using System.Collections.Concurrent;
using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.Mapping.Core;

public class MappingConfiguration
{
  private readonly ConcurrentDictionary<(Type, Type), IMappingExpression> _mappings = new();

  // =========================================================
  // REGISTER (DETERMINISTIC)
  // =========================================================
  public void Register<TSource, TDestination>(MappingExpression<TSource, TDestination> expression)
  {
    var key = (typeof(TSource), typeof(TDestination));

    // deterministic rule: last write wins BUT explicit logging hook point
    _mappings.AddOrUpdate(key,
      expression,
      (_, _) => expression);
  }

  // =========================================================
  // TYPED RESOLUTION (FAST PATH)
  // =========================================================
  public bool TryGetMapping<TSource, TDestination>(
      out MappingExpression<TSource, TDestination>? expression)
  {
    if (_mappings.TryGetValue((typeof(TSource), typeof(TDestination)), out var obj)
        && obj is MappingExpression<TSource, TDestination> typed)
    {
      expression = typed;
      return true;
    }

    expression = null;
    return false;
  }

  // =========================================================
  // RUNTIME RESOLUTION (NO GENERICS)
  // =========================================================
  public bool TryGetMapping(Type source, Type dest, out IMappingExpression? expression)
  {
    if (_mappings.TryGetValue((source, dest), out var expr))
    {
      expression = expr;
      return true;
    }

    expression = null;
    return false;
  }

  public bool HasMapping(Type source, Type dest)
      => _mappings.ContainsKey((source, dest));
}