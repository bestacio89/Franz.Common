using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mapping.Profiles;

  public abstract class FranzMapProfile : IFranzMapProfile
  {
    public abstract void Configure(MappingConfiguration config);

    protected MappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
      return new MappingExpression<TSource, TDestination>();
    }
  }
