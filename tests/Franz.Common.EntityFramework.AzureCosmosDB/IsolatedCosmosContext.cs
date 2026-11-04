using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.AzureCosmosDB.Tests;

public sealed class IsolatedCosmosContext : IAsyncDisposable
{
  private readonly IServiceProvider _provider;
  private int _disposed;

  public IsolatedCosmosContext(IServiceProvider provider)
  {
    _provider = provider ?? throw new ArgumentNullException(nameof(provider));
  }

  public IServiceScope CreateScope()
  {
    ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    return _provider.CreateScope();
  }

  public async Task CleanUpAsync()
  {
    try
    {
      await using var scope = _provider.CreateAsyncScope();
      var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

      // Hard deletion keeps each test database isolated and avoids metadata reuse.
      await db.Database.EnsureDeletedAsync();
    }
    finally
    {
      // CosmosClient owns long-lived sockets and background workers. Dispose the
      // per-test service provider even when database cleanup fails.
      await DisposeAsync();
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (Interlocked.Exchange(ref _disposed, 1) != 0)
    {
      return;
    }

    if (_provider is IAsyncDisposable asyncDisposable)
    {
      await asyncDisposable.DisposeAsync();
      return;
    }

    (_provider as IDisposable)?.Dispose();
  }
}
