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
  public readonly OracleContainer Container = new OracleBuilder("gvenzl/oracle-free:23-slim-faststart")
      .Build();

  public async Task InitializeAsync() => await Container.StartAsync();
  public async Task DisposeAsync() => await Container.DisposeAsync();

  public IServiceProvider BuildServiceProvider(
      string? domainId = null,
      Action<IServiceCollection>? extraRegistrations = null)
  {
    var services = new ServiceCollection();

    var configDict = new Dictionary<string, string?>
    {
      ["Database:ServerName"] = Container.Hostname,
      ["Database:Port"] = Container.GetMappedPublicPort(1521).ToString(),
      ["Database:DatabaseName"] = "FREEPDB1", // Oracle Service Name
      ["Database:UserName"] = "system",
      ["Database:Password"] = "oracle"
    };

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(configDict)
        .Build();

    // Infrastructure
    services.AddSingleton(Substitute.For<IDispatcher>());
    services.AddLogging();
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();
    services.AddTransient(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));

    var domainAccessor = Substitute.For<IDomainContextAccessor>();
    domainAccessor.GetCurrentDomainId().Returns(domainId != null ? Guid.Parse(domainId) : null);
    services.AddSingleton(domainAccessor);

    // SUT
    services.AddOracleDatabase<TestDbContext>(configuration);

    extraRegistrations?.Invoke(services);
    return services.BuildServiceProvider();
  }
}