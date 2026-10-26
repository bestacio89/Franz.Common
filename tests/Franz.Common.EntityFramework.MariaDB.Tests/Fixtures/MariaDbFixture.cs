using Testcontainers.MariaDb;
using Xunit;

namespace Franz.Common.EntityFramework.MariaDB.Tests.Fixtures;

public sealed class MariaDbFixture : IAsyncLifetime
{
  private readonly MariaDbContainer _container = new MariaDbBuilder("mariadb:11.4")
      .WithDatabase("franz_provider_tests")
      .WithUsername("franz_test")
      .WithPassword("franz_test_password")
      .Build();

  public string ConnectionString => _container.GetConnectionString();

  public Task InitializeAsync() => _container.StartAsync();

  public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class MariaDbCollection : ICollectionFixture<MariaDbFixture>
{
  public const string Name = "MariaDB provider qualification";
}
