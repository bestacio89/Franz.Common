using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
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
            Microting.EntityFrameworkCore.MySql.Infrastructure.ServerVersion.Create(
                new Version(11, 4, 0),
                Microting.EntityFrameworkCore.MySql.Infrastructure.ServerType.MariaDb))
        .Options;

    return options;
  }

  protected TestDbContext CreateContext() =>
      new(CreateOptions(), Substitute.For<IDispatcher>());
}
