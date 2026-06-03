using DotNet.Testcontainers.Builders;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.MsSql;

namespace Franz.Common.EntityFramework.SQLServer.Tests;

public class SqlServerFixture : IAsyncLifetime
{
  public readonly MsSqlContainer Container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
      .WithPassword("Strong_Password_123!") // SQL Server requires complexity
      .Build();

  public async Task InitializeAsync() => await Container.StartAsync();
  public async Task DisposeAsync() => await Container.DisposeAsync();

  public IServiceProvider BuildServiceProvider(
      string? domainId = null,
      Action<IServiceCollection>? extraRegistrations = null)
  {
    var services = new ServiceCollection();

    // Map Testcontainer properties to DatabaseOptions
    var configDict = new Dictionary<string, string?>
    {
      ["Database:ServerName"] = Container.Hostname,
      ["Database:Port"] = Container.GetMappedPublicPort(1433).ToString(),
      ["Database:DatabaseName"] = "franz_{dbName}",
      ["Database:UserName"] = "sa",
      ["Database:Password"] = "Strong_Password_123!",
      ["Database:SslMode"] = "Preferred" // Will map to Mandatory via GetMap
    };

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(configDict)
        .Build();

    // Infrastructure Dependencies
    var dispatcher = Substitute.For<IDispatcher>();
    services.AddSingleton(dispatcher);
    services.AddLogging();
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();
    services.AddTransient(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));

    // Multi-tenancy Mock
    var domainAccessor = Substitute.For<IDomainContextAccessor>();
    domainAccessor.GetCurrentDomainId().Returns(domainId != null ? Guid.Parse(domainId) : null);
    services.AddSingleton(domainAccessor);

    // SUT: SQL Server Registration
    services.AddSqlServerDatabase<TestDbContext>(configuration);

    extraRegistrations?.Invoke(services);

    return services.BuildServiceProvider();
  }
}