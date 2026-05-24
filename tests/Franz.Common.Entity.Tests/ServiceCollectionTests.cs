using System;
using System.Collections.Generic;
using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.EntityFramework.Repositories;
using Franz.Common.EntityFramework.Extensions;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Franz.Common.EntityFramework.Tests;

public class ServiceCollectionExtensions_EfRegistrationTests
{
  private sealed class SimpleEntity : Entity<int>
  {
    public string Name { get; set; } = string.Empty;
  }

  private sealed class SimpleCtx : DbContextBase
  {
    public SimpleCtx(DbContextOptions<SimpleCtx> options)
        : base(options, new Mock<IDispatcher>().Object)
    {
    }

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

    var cfg = new ConfigurationBuilder()
        .AddInMemoryCollection(dict!)
        .Build();

    var services = new ServiceCollection();

    services.AddDatabaseOptions(cfg);

    using var sp = services.BuildServiceProvider();

    sp.GetRequiredService<IOptions<DatabaseOptions>>()
        .Value.DatabaseName
        .Should()
        .Be("franz_test");
  }
}