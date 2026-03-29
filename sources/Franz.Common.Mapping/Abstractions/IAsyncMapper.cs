using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mapping.Abstractions;

public interface IAsyncMapper<in TSource, TDestination>
{
    ValueTask<TDestination> MapAsync(TSource source, CancellationToken cancellationToken = default);
}
