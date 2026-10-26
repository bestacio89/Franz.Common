using Franz.Common.EntityFramework.MariaDB.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.MariaDB.Tests.Transactions;

[Collection(MariaDbCollection.Name)]
public sealed class TransactionTests(MariaDbFixture database) : MariaDbTestBase(database)
{
  [Fact]
  public async Task Rollback_RemovesChangesFromPersistedState()
  {
    var marker = $"rollback-{Guid.NewGuid():N}";
    await using (var context = CreateContext())
    {
      await context.Database.EnsureCreatedAsync();
      await using var transaction = await context.Database.BeginTransactionAsync();
      context.Entities.Add(new TestEntity { Name = marker });
      await context.SaveChangesAsync();
      await transaction.RollbackAsync();
    }

    await using var verify = CreateContext();
    Assert.False(await verify.Entities.IgnoreQueryFilters().AnyAsync(x => x.Name == marker));
  }

  [Fact]
  public async Task Commit_PersistsChangesVisibleFromANewContext()
  {
    var marker = $"commit-{Guid.NewGuid():N}";
    await using (var context = CreateContext())
    {
      await context.Database.EnsureCreatedAsync();
      await using var transaction = await context.Database.BeginTransactionAsync();
      context.Entities.Add(new TestEntity { Name = marker });
      await context.SaveChangesAsync();
      await transaction.CommitAsync();
    }

    await using var verify = CreateContext();
    Assert.True(await verify.Entities.AnyAsync(x => x.Name == marker));
  }
}
