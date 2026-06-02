using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.Mapping.Core;

public class MappingService(IFranzMapper mapper) : IMappingService, IAsyncDisposable
{
  private readonly IFranzMapper _mapper = mapper;
  private int _disposedState; // 0 = active, 1 = disposed

  public TDestination Map<TSource, TDestination>(TSource source)
  {
    if (Volatile.Read(ref _disposedState) == 1)
      throw new ObjectDisposedException(nameof(MappingService));

    return _mapper.Map<TSource, TDestination>(source);
  }

  public ValueTask<TDestination> MapAsync<TSource, TDestination>(
    TSource source,
    CancellationToken cancellationToken = default)
  {
    if (Volatile.Read(ref _disposedState) == 1)
      throw new ObjectDisposedException(nameof(MappingService));

    cancellationToken.ThrowIfCancellationRequested();

    return ValueTask.FromResult(
        _mapper.Map<TSource, TDestination>(source));
  }

  public ValueTask DisposeAsync()
  {
    if (Interlocked.Exchange(ref _disposedState, 1) == 0)
    {
      GC.SuppressFinalize(this);
    }
    return ValueTask.CompletedTask;
  }
}