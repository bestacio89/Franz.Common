using Franz.Common.EntityFramework.Tests;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.Integration.Tests.EntityFramework.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework;

public class SqlServerIntegrationTests : DatabaseIntegrationTestBase, IClassFixture<SqlServerFixture>
{
  private readonly SqlServerFixture _fixture;
  public SqlServerIntegrationTests(SqlServerFixture fixture) => _fixture = fixture;

  protected override void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
      => services.AddSqlServerDatabase<TestDbContext>(configuration);

  protected override string GetConnectionString(DbContext context)
      => context.Database.GetDbConnection().ConnectionString;

  protected override IConfiguration BuildConfiguration(string dbName) => new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Database:ServerName"] = _fixture.Container.Hostname,
        ["Database:Port"] = _fixture.Container.GetMappedPublicPort(1433).ToString(),
        ["Database:DatabaseName"] = dbName,
        ["Database:UserName"] = "sa",
        ["Database:Password"] = "Password123!" // SQL Server needs complex passwords
      }).Build();
}
