using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.AzureCosmosDB.Tests;

public sealed class IsolatedCosmosContext : IAsyncDisposable
{
  private readonly IServiceProvider _provider;

  public IsolatedCosmosContext(IServiceProvider provider)
  {
    _provider = provider;
  }

  public IServiceScope CreateScope() => _provider.CreateScope();

  public async Task CleanUpAsync()
  {
    using var scope = _provider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

    // HARD DELETE (required for Cosmos correctness)
    await db.Database.EnsureDeletedAsync();

    // 🔥 IMPORTANT: allow emulator to settle metadata state
    await Task.Delay(300);
  }

  public ValueTask DisposeAsync()
  {
    (_provider as IDisposable)?.Dispose();
    return ValueTask.CompletedTask;
  }
}