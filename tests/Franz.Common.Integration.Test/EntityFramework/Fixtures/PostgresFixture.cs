using DotNet.Testcontainers.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
  public PostgreSqlContainer Container { get; } =
      new PostgreSqlBuilder("postgres:latest")
          .WithDatabase("testdb")
          .WithUsername("postgres")
          .WithPassword("password")
          .WithWaitStrategy(
              Wait.ForUnixContainer()
                  .UntilMessageIsLogged("database system is ready to accept connections"))
          .Build();
  public async Task InitializeAsync() => await Container.StartAsync();
  public async Task DisposeAsync() => await Container.DisposeAsync();
}
