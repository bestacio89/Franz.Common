using Franz.Common.EntityFramework;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;
using Franz.Common.Mediator.Dispatchers;

namespace Franz.Common.Integration.Tests.EntityFramework;

public abstract class DatabaseIntegrationTestBase
{
  protected ILogger Logger { get; private set; }  // Add protected logger

  protected abstract void ConfigureDatabase(IServiceCollection services, IConfiguration configuration);
  protected abstract string GetConnectionString(DbContext context);

  private void AddCommonMocks(IServiceCollection services)
  {
    // Franz 1.7.8 Hardening: DbContext now requires a dispatcher for Outbox/Domain Events
    var mockDispatcher = new Mock<IDispatcher>();
    services.AddSingleton(mockDispatcher.Object);
  }

  private IServiceProvider BuildServiceProviderWithLogger(IServiceCollection services)
  {
    // Add logging services
    services.AddLogging(config => config.AddConsole());
    var provider = services.BuildServiceProvider();

    // Create a logger for use in the tests
    Logger = provider.GetRequiredService<ILogger<DatabaseIntegrationTestBase>>();
    return provider;
  }

  [Fact]
  public async Task Database_ShouldConnectAndIdentifyCorrectSchema()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = BuildConfiguration();

    AddCommonMocks(services);
    ConfigureDatabase(services, configuration);

    var provider = BuildServiceProviderWithLogger(services);

    // Act
    using var scope = provider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DbContextBase>();
    var canConnect = await context.Database.CanConnectAsync();

    // Assert
    Assert.True(canConnect, $"Failed to connect to {this.GetType().Name}");
  }

  [Fact]
  public void MultiTenancy_ShouldSubstituteDatabaseName()
  {
    // Arrange
    var services = new ServiceCollection();

    AddCommonMocks(services);
    var tenantId = Guid.CreateVersion7(); // This is a Guid object

    var mockAccessor = new Mock<IDomainContextAccessor>();
    mockAccessor.Setup(x => x.GetCurrentDomainId()).Returns(tenantId);
    services.AddSingleton(mockAccessor.Object);

    var configuration = BuildConfiguration("{dbName}");
    ConfigureDatabase(services, configuration);

    var provider = BuildServiceProviderWithLogger(services);

    // Act
    using var scope = provider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DbContextBase>();
    var connectionString = GetConnectionString(context);

    // Assert
    Assert.Contains(tenantId.ToString(), connectionString);
  }

  protected abstract IConfiguration BuildConfiguration(string dbName = "library");
}