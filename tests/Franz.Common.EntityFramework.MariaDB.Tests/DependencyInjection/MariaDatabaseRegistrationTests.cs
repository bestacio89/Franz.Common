using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Franz.Common.EntityFramework.MariaDB.Tests.Fixtures;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
}
