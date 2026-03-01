using DotNet.Testcontainers.Builders;
using System.Threading.Tasks;
using Testcontainers.MsSql; // Ensure you have the Testcontainers.MsSql NuGet package
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework.Fixtures
{
  public class SqlServerFixture : IAsyncLifetime
  {
    // Use MsSqlContainer directly
    public MsSqlContainer Container { get; }

    public SqlServerFixture()
    {
      // The modern way: Use the specific Builder for the module
      Container = new MsSqlBuilder()
          .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
          .WithPassword("Password123!")
          .WithCleanUp(true)
          .Build();
    }

    public async Task InitializeAsync()
    {
      await Container.StartAsync();
      // Note: MsSqlBuilder has a built-in wait strategy, 
      // so you usually don't need Task.Delay(2000) anymore.
    }

    public async Task DisposeAsync()
    {
      await Container.StopAsync();
      await Container.DisposeAsync();
    }
  }
}