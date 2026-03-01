using Franz.Common.EntityFramework.Tests;
using Franz.Common.Integration.Tests.EntityFramework;
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.Integration.Tests.EntityFramework.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class PostgresIntegrationTests : DatabaseIntegrationTestBase, IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;
  public PostgresIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

  protected override void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
      => services.AddPostgresDatabase<TestDbContext>(configuration);

  protected override string GetConnectionString(DbContext context)
      => context.Database.GetDbConnection().ConnectionString;

  protected override IConfiguration BuildConfiguration(string dbName) => new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Database:ServerName"] = _fixture.Container.Hostname,
        ["Database:Port"] = _fixture.Container.GetMappedPublicPort(5432).ToString(),
        ["Database:DatabaseName"] = dbName,
        ["Database:UserName"] = "postgres",
        ["Database:Password"] = "password"
      }).Build();
}