using DotNet.Testcontainers.Builders;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Business.Extensions;
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public class PostgresFixture : IAsyncLifetime
{
  public readonly PostgreSqlContainer Container = new PostgreSqlBuilder()
      .WithDatabase("franz_test")
      .WithUsername("postgres")
      .WithPassword("password")
      .Build();

  public async Task InitializeAsync() => await Container.StartAsync();
  public async Task DisposeAsync() => await Container.DisposeAsync();

  public IServiceProvider BuildServiceProvider(
      string? domainId = null,
      Action<IServiceCollection>? extraRegistrations = null)
  {
    var services = new ServiceCollection();

    // Configuration Mock
    var configDict = new Dictionary<string, string?>
    {
      ["Database:ServerName"] = Container.Hostname,
      ["Database:Port"] = Container.GetMappedPublicPort(5432).ToString(),
      ["Database:DatabaseName"] = "test_{dbName}", // Testing pattern replacement
      ["Database:UserName"] = "postgres",
      ["Database:Password"] = "password"
    };
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

    // Dependencies
    var dispatcher = Substitute.For<IDispatcher>();
    services.AddSingleton(dispatcher);
    services.AddLogging();
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();
    services.AddTransient(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));
    // Multi-tenancy Mock
    var domainAccessor = Substitute.For<IDomainContextAccessor>();
    domainAccessor.GetCurrentDomainId().Returns(domainId != null ? Guid.Parse(domainId) : (Guid?)null);
    services.AddSingleton(domainAccessor);

    // System Under Test (SUT)
    services.AddPostgresDatabase<TestDbContext>(configuration);

    extraRegistrations?.Invoke(services);

    return services.BuildServiceProvider();
  }
}