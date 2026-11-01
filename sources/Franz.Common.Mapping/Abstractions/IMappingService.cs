using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mapping.Abstractions;

public interface IMappingService
{
    [RequiresUnreferencedCode("The configured IFranzMapper may inspect runtime model members. Use a generated IMapper<TSource, TDestination> for trimming.")]
    [RequiresDynamicCode("The configured IFranzMapper may compile runtime mapping plans. Use a generated IMapper<TSource, TDestination> for Native AOT.")]
    TDestination Map<TSource, TDestination>(TSource source);

    [RequiresUnreferencedCode("The configured IFranzMapper may inspect runtime model members. Use a generated IMapper<TSource, TDestination> for trimming.")]
    [RequiresDynamicCode("The configured IFranzMapper may compile runtime mapping plans. Use a generated IMapper<TSource, TDestination> for Native AOT.")]
    ValueTask<TDestination> MapAsync<TSource, TDestination>(TSource source, CancellationToken cancellationToken = default);
}
