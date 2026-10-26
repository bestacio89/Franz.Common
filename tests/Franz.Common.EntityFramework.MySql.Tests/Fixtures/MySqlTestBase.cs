using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using NSubstitute;

namespace Franz.Common.EntityFramework.MySql.Tests.Fixtures;

public abstract class MySqlTestBase(MySqlFixture database)
{
  protected MySqlFixture Database { get; } = database;

  protected DbContextOptions<TestDbContext> CreateOptions()
  {
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseMySql(
            Database.ConnectionString,
            ServerVersion.Create(new Version(8, 4, 0), ServerType.MySql))
        .Options;

    return options;
  }

  protected TestDbContext CreateContext() =>
      new(CreateOptions(), Substitute.For<IDispatcher>());
}
