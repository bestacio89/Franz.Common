
using System.Collections.Generic;
using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
namespace Franz.Common.Integration.Tests.EntityFramework;
public class ServiceCollectionExtensions_EfRegistrationTests
{
  private sealed class SimpleEntity : Entity<int>
  {
    
    public string Name { get; set; } = "";
    
  }

  private sealed class SimpleCtx : DbContext
  {
    public SimpleCtx(DbContextOptions<SimpleCtx> o) : base(o) { }
    public DbSet<SimpleEntity> Entities => Set<SimpleEntity>();
  }

  [Fact]
  public void AddDatabaseOptions_binds_from_configuration()
  {
    var dict = new Dictionary<string, string?>
    {
      ["Database:DatabaseName"] = "franz_test",
      ["Database:ServerName"] = "localhost"
    };

    var cfg = new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    var services = new ServiceCollection();

    services.AddDatabaseOptions(cfg);

    using var sp = services.BuildServiceProvider();
    sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.DatabaseName.Should().Be("franz_test");
  }

  [Fact]
  public void AddGenericRepositories_registers_IReadRepository_for_all_DbSets()
  {
    var services = new ServiceCollection();
    services.AddDbContext<SimpleCtx>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
    services.AddGenericRepositories<SimpleCtx>();

    using var sp = services.BuildServiceProvider();
    var repo = sp.GetService<IReadRepository<SimpleEntity>>();
    repo.Should().NotBeNull();
  }
}
