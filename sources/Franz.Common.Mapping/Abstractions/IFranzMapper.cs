using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mapping.Abstractions;
public interface IFranzMapper
{
  TDestination Map<TSource, TDestination>(TSource source);
}