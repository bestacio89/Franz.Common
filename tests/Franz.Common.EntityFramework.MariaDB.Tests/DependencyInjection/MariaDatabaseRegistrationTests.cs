using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Franz.Common.EntityFramework.MariaDB.Tests.Fixtures;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using NSubstitute;

namespace Franz.Common.EntityFramework.MariaDB.Tests.DependencyInjection;

[Collection(MariaDbCollection.Name)]
public sealed class MariaDatabaseRegistrationTests(MariaDbFixture database)
{
  [Fact]
  public async Task AddMariaDatabase_ResolvesConfiguredDbContextAndConnects()
  {
    var connection = new MySqlConnectionStringBuilder(database.ConnectionString);
    var settings = new Dictionary<string, string?>
    {
      ["Database:ServerName"] = connection.Server,
      ["Database:DatabaseName"] = connection.Database,
      ["Database:UserName"] = connection.UserID,
      ["Database:Password"] = connection.Password,
      ["Database:Port"] = connection.Port.ToString(),
      ["Database:SslMode"] = "None"
    };

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(settings)
        .Build();

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton(Substitute.For<IDispatcher>());
    services.AddMariaDatabase<TestDbContext>(configuration);

    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

    Assert.True(await context.Database.CanConnectAsync());
    Assert.Same(context, scope.ServiceProvider.GetRequiredService<DbContextBase>());
  }

  [Fact]
  public void AddMariaDatabase_UsesTenantIdAndNeverFallsBackToDomainId()
  {
    var connection = new MySqlConnectionStringBuilder(database.ConnectionString);
    var tenantId = Guid.NewGuid();
    var domainId = Guid.NewGuid();
    var tenantContext = new TenantExecutionContext(
        new TenantInfo(tenantId, "tenant"),
        domainId,
        TenantResolutionSource.Unknown,
        DateTimeOffset.UtcNow);
    var accessor = new StubTenantExecutionContextAccessor(tenantContext);
    var settings = new Dictionary<string, string?>
    {
      ["Database:ServerName"] = connection.Server,
      ["Database:DatabaseName"] = "franz_{dbName}",
      ["Database:IsolationMode"] = "TenantDatabase",
      ["Database:UserName"] = connection.UserID,
      ["Database:Password"] = connection.Password,
      ["Database:Port"] = connection.Port.ToString(),
      ["Database:SslMode"] = "None"
    };

    var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton(Substitute.For<IDispatcher>());
    services.AddSingleton<ITenantExecutionContextAccessor>(accessor);
    services.AddMariaDatabase<TestDbContext>(configuration);

    using var provider = services.BuildServiceProvider();
    var connectionString = provider.GetRequiredService<TestDbContext>().Database.GetConnectionString();

    Assert.Contains($"Database=franz_{tenantId:D}", connectionString);
    Assert.DoesNotContain(domainId.ToString(), connectionString);
  }

  private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current)
      : ITenantExecutionContextAccessor
  {
    public TenantExecutionContext? Current { get; private set; } = current;
    public void Initialize(TenantExecutionContext context) => Current = context;
    public void Clear() => Current = null;
  }
}
