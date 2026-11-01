using FluentAssertions;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.Oracle.Tests;

public class OraclePersistenceTests : IClassFixture<OracleFixture>
{
  private readonly OracleFixture _fixture;

  public OraclePersistenceTests(OracleFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task Should_Register_Oracle_Context_And_Connect()
  {
    var sp = _fixture.BuildServiceProvider();
    using var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

    var canConnect = await db.Database.CanConnectAsync();
    canConnect.Should().BeTrue();
  }

  [Fact]
  public void AddOracleDatabase_UsesTenantIdWhenTenantIsolationIsEnabled()
  {
    var tenantId = Guid.NewGuid();
    var domainId = Guid.NewGuid();
    var context = new TenantExecutionContext(
        new TenantInfo(tenantId, "tenant"),
        domainId,
        TenantResolutionSource.Unknown,
        DateTimeOffset.UtcNow);
    var accessor = new StubTenantExecutionContextAccessor(context);

    var provider = _fixture.BuildServiceProvider(
        domainId.ToString(),
        services => services.AddSingleton<ITenantExecutionContextAccessor>(accessor),
        databaseName: "FRANZ_{dbName}",
        isolationMode: "TenantDatabase");

    var connectionString = provider.GetRequiredService<TestDbContext>().Database.GetConnectionString();

    connectionString.Should().Contain($"/FRANZ_{tenantId:D}");
    connectionString.Should().NotContain(domainId.ToString());
  }

  [Fact]
  public void AddOracleDatabase_UsesDomainIdOnlyWhenDomainIsolationIsExplicit()
  {
    var domainId = Guid.NewGuid();
    var provider = _fixture.BuildServiceProvider(
        domainId.ToString(),
        databaseName: "FRANZ_{dbName}",
        isolationMode: "DomainDatabase");

    var connectionString = provider.GetRequiredService<TestDbContext>().Database.GetConnectionString();

    connectionString.Should().Contain($"/FRANZ_{domainId:D}");
  }

  private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current)
      : ITenantExecutionContextAccessor
  {
    public TenantExecutionContext? Current { get; private set; } = current;
    public void Initialize(TenantExecutionContext context) => Current = context;
    public void Clear() => Current = null;
  }
}
