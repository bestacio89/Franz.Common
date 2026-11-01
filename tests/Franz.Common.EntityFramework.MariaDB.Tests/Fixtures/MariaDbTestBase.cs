using Franz.Common.EntityFramework.ProviderContract.Tests;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using NSubstitute;

namespace Franz.Common.EntityFramework.MariaDB.Tests.Fixtures;

public abstract class MariaDbTestBase(MariaDbFixture database)
{
  protected MariaDbFixture Database { get; } = database;

  protected DbContextOptions<TestDbContext> CreateOptions()
  {
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseMySql(
            Database.ConnectionString,
            ServerVersion.Create(new Version(11, 4, 0), ServerType.MariaDb))
        .Options;

    return options;
  }

  protected TestDbContext CreateContext(Guid? tenantId = null) =>
      new(CreateOptions(), Substitute.For<IDispatcher>(),
          tenantContextAccessor: TenantExecutionContextAccessor.ForTenant(tenantId));
}
