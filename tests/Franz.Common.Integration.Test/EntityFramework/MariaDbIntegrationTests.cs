using Franz.Common.EntityFramework.Tests;
using Franz.Common.Integration.Tests.EntityFramework.Fixtures;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework;

public class MariaDbIntegrationTests : DatabaseIntegrationTestBase, IClassFixture<MariaDbFixture>
{
  private readonly MariaDbFixture _fixture;
  public MariaDbIntegrationTests(MariaDbFixture fixture) => _fixture = fixture;

  protected override void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
      => services.AddMariaDatabase<TestDbContext>(configuration);

  protected override string GetConnectionString(DbContext context)
      => context.Database.GetDbConnection().ConnectionString;

  protected override IConfiguration BuildConfiguration(string dbName) => new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Database:ServerName"] = _fixture.Container.Hostname,
        ["Database:Port"] = _fixture.Container.GetMappedPublicPort(3306).ToString(),
        ["Database:DatabaseName"] = dbName,
        ["Database:UserName"] = "root",
        ["Database:Password"] = "password"
      }).Build();
}