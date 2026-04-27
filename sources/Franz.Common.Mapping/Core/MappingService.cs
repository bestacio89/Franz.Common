using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.Mapping.Core;

public class MappingService(IServiceProvider serviceProvider) : IMappingService, IAsyncDisposable
{
  private readonly ConcurrentDictionary<Type, object> _mapperCache = new();
  private readonly SemaphoreSlim _semaphore = new(1, 1);
  private bool _disposed;

  public TDestination Map<TSource, TDestination>(TSource source)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var mapper = GetOrCreateMapper<IMapper<TSource, TDestination>>(
        typeof(IMapper<TSource, TDestination>));

    return mapper.Map(source);
  }

  public async ValueTask<TDestination> MapAsync<TSource, TDestination>(
      TSource source,
      CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    var mapper = await GetOrCreateMapperAsync<IAsyncMapper<TSource, TDestination>>(
        typeof(IAsyncMapper<TSource, TDestination>),
        cancellationToken
    ).ConfigureAwait(false);

    return await mapper.MapAsync(source, cancellationToken).ConfigureAwait(false);
  }

  private TMapper GetOrCreateMapper<TMapper>(Type mapperType)
      where TMapper : notnull
  {
    if (_mapperCache.TryGetValue(mapperType, out var cachedMapper))
    {
      return (TMapper)cachedMapper;
    }

    _semaphore.Wait();
    try
    {
      if (_mapperCache.TryGetValue(mapperType, out var map))
      {
        return (TMapper)map;
      }

      var mapper = serviceProvider.GetRequiredService<TMapper>();
      _mapperCache[mapperType] = mapper!;
      return mapper;
    }
    finally
    {
      _semaphore.Release();
    }
  }

  private async ValueTask<TMapper> GetOrCreateMapperAsync<TMapper>(
      Type mapperType,
      CancellationToken cancellationToken)
      where TMapper : notnull
  {
    if (_mapperCache.TryGetValue(mapperType, out var cachedMapper))
    {
      return (TMapper)cachedMapper;
    }

    await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      if (_mapperCache.TryGetValue(mapperType, out var map))
      {
        return (TMapper)map;
      }

      var mapper = serviceProvider.GetRequiredService<TMapper>();
      _mapperCache[mapperType] = mapper!;
      return mapper;
    }
    finally
    {
      _semaphore.Release();
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed) return;

    _disposed = true;

    await _semaphore.WaitAsync().ConfigureAwait(false);
    try
    {
      _mapperCache.Clear();
    }
    finally
    {
      _semaphore.Release();
      _semaphore.Dispose();
    }

    GC.SuppressFinalize(this);
  }
}