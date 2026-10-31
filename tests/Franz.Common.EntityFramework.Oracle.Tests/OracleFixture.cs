using DotNet.Testcontainers.Builders;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.EntityFramework.Oracle.Extensions;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.Oracle;

namespace Franz.Common.EntityFramework.Oracle.Tests;

public class OracleFixture : IAsyncLifetime
{
  private readonly SemaphoreSlim _databaseInitializationLock = new(1, 1);
  private bool _databaseInitialized;

  public async Task EnsureDatabaseCreatedAsync(Microsoft.EntityFrameworkCore.DbContext context)
  {
    if (_databaseInitialized)
      return;

    await _databaseInitializationLock.WaitAsync();
    try
    {
      if (_databaseInitialized)
        return;

      await context.Database.EnsureCreatedAsync();
      _databaseInitialized = true;
    }
    finally
    {
      _databaseInitializationLock.Release();
    }
  }

  public readonly OracleContainer Container = new OracleBuilder("gvenzl/oracle-free:23-slim-faststart")
      .WithWaitStrategy(Wait.ForUnixContainer()
          .UntilMessageIsLogged(".*DATABASE IS READY TO USE.*")
          .UntilCommandIsCompleted("sqlplus -S system/oracle@//localhost:1521/FREEPDB1 <<EOF\nEXIT;\nEOF"))
      .Build();

  public async Task InitializeAsync() => await Container.StartAsync();
  public async Task DisposeAsync() => await Container.DisposeAsync();

  public IServiceProvider BuildServiceProvider(
      string? domainId = null,
      Action<IServiceCollection>? extraRegistrations = null,
      string databaseName = "FREEPDB1",
      string isolationMode = "SharedDatabase")
  {
    var services = new ServiceCollection();

    var configDict = new Dictionary<string, string?>
    {
      ["Database:ServerName"] = Container.Hostname,
      ["Database:Port"] = Container.GetMappedPublicPort(1521).ToString(),
      ["Database:DatabaseName"] = databaseName,
      ["Database:IsolationMode"] = isolationMode,
      ["Database:UserName"] = "system",
      ["Database:Password"] = "oracle"
    };

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(configDict)
        .Build();

    services.AddSingleton(Substitute.For<IDispatcher>());
    services.AddLogging();
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();
    services.AddTransient(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));

    var domainAccessor = Substitute.For<IDomainContextAccessor>();
    domainAccessor.GetCurrentDomainId().Returns(domainId != null ? Guid.Parse(domainId) : null);
    services.AddSingleton(domainAccessor);

    extraRegistrations?.Invoke(services);
    services.AddOracleDatabase<TestDbContext>(configuration);

    return services.BuildServiceProvider();
  }
}
