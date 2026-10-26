using Testcontainers.MySql;
using Xunit;

namespace Franz.Common.EntityFramework.MySql.Tests.Fixtures;

public sealed class MySqlFixture : IAsyncLifetime
{
  private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
      .WithDatabase("franz_provider_tests")
      .WithUsername("franz_test")
      .WithPassword("franz_test_password")
      .Build();

  public string ConnectionString => _container.GetConnectionString();

  public Task InitializeAsync() => _container.StartAsync();

  public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class MySqlCollection : ICollectionFixture<MySqlFixture>
{
  public const string Name = "MySQL 8.4 provider qualification";
}
