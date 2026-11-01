using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mapping.Abstractions;
public interface IFranzMapper
{
  [RequiresUnreferencedCode("The reflection-based mapper inspects runtime model members. Use a generated IMapper<TSource, TDestination> implementation for trimming and Native AOT.")]
  [RequiresDynamicCode("The reflection-based mapper creates runtime mapping plans. Use a generated IMapper<TSource, TDestination> implementation for Native AOT.")]
  TDestination Map<TSource, TDestination>([DisallowNull] TSource source);
}