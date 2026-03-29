using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mapping.Abstractions;

public interface IMappingService
{
    TDestination Map<TSource, TDestination>(TSource source);
    ValueTask<TDestination> MapAsync<TSource, TDestination>(TSource source, CancellationToken cancellationToken = default);
}
