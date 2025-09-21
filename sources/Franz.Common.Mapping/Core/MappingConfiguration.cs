using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mapping.Core;
public class MappingConfiguration
{
  private readonly ConcurrentDictionary<(Type, Type), object> _mappings = new();

  public void Register<TSource, TDestination>(MappingExpression<TSource, TDestination> expression)
  {
    _mappings[(typeof(TSource), typeof(TDestination))] = expression;
  }

  internal bool TryGetMapping<TSource, TDestination>(out MappingExpression<TSource, TDestination>? expression)
  {
    if (_mappings.TryGetValue((typeof(TSource), typeof(TDestination)), out var obj)
        && obj is MappingExpression<TSource, TDestination> mapping)
    {
      expression = mapping;
      return true;
    }

    expression = null;
    return false;
  }
}