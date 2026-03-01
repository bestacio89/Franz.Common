using Franz.Common.EntityFramework;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework;

public abstract class DatabaseIntegrationTestBase
{
  protected abstract void ConfigureDatabase(IServiceCollection services, IConfiguration configuration);
  protected abstract string GetConnectionString(DbContext context);

  [Fact]
  public async Task Database_ShouldConnectAndIdentifyCorrectSchema()
  {
    // Arrange
    var services = new ServiceCollection();
    var configuration = BuildConfiguration();

    services.AddLogging();
    ConfigureDatabase(services, configuration);

    var provider = services.BuildServiceProvider();

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
    var tenantId = Guid.CreateVersion7(); // This is a Guid object

    var mockAccessor = new Mock<IDomainContextAccessor>();
    mockAccessor.Setup(x => x.GetCurrentDomainId()).Returns(tenantId);
    services.AddSingleton(mockAccessor.Object);

    // We use the pattern "{dbName}" which your extension replaces
    var configuration = BuildConfiguration("{dbName}");
    ConfigureDatabase(services, configuration);

    var provider = services.BuildServiceProvider();

    // Act
    using var scope = provider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DbContextBase>();
    var connectionString = GetConnectionString(context);

    // Assert
    // Explicitly convert to string to ensure we are comparing strings to strings
    Assert.Contains(tenantId.ToString(), connectionString);
  }

  protected abstract IConfiguration BuildConfiguration(string dbName = "library");
}