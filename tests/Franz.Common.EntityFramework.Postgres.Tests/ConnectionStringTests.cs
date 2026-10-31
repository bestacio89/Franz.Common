using FluentAssertions;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public class ConnectionStringTests : IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;

  public ConnectionStringTests(PostgresFixture fixture) => _fixture = fixture;

  [Fact]
  public void AddPostgresDatabase_WithDomainId_ShouldReplacePattern()
  {
    var domainId = Guid.NewGuid();
    var sp = _fixture.BuildServiceProvider(
        domainId.ToString(),
        databaseName: "test_{dbName}",
        isolationMode: "DomainDatabase");

    var db = sp.GetRequiredService<TestDbContext>();
    var connectionString = db.Database.GetConnectionString();

    connectionString.Should().Contain($"Database=test_{domainId:D}");
  }

  [Fact]
  public void AddPostgresDatabase_WithoutDomainId_ShouldFailClosed()
  {
    var sp = _fixture.BuildServiceProvider(
        databaseName: "test_{dbName}",
        isolationMode: "DomainDatabase");

    var act = () => sp.GetRequiredService<TestDbContext>();

    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*DomainDatabase routing requires a domain context*");
  }
}
