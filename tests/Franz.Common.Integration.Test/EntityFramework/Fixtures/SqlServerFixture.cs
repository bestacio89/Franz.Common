using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.MsSql;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework.Fixtures;

public class SqlServerFixture : IAsyncLifetime
{
  public MsSqlContainer Container { get; } = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
      .Build();

  public async Task InitializeAsync() => await Container.StartAsync();
  public async Task DisposeAsync() => await Container.DisposeAsync();
}
