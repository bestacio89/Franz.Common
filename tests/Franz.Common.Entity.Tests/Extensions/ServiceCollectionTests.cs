using Franz.Common.EntityFramework.Configuration;
using Franz.Common.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Franz.Common.EntityFramework.Extensions;
namespace Franz.Common.EntityFramework.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
  [Fact]
  public void AddDatabaseOptions_ConfiguresOptions()
  {
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("Database:DatabaseName", "TestDb") })
        .Build();

    var services = new ServiceCollection();
    services.AddDatabaseOptions(config);

    var sp = services.BuildServiceProvider();
    var options = sp.GetService<IOptions<DatabaseOptions>>();
    Assert.NotNull(options);
    Assert.Equal("TestDb", options!.Value.DatabaseName);
  }

  [Fact]
  public void AddDatabaseOptions_Throws_WhenConfigMissing()
  {
    var services = new ServiceCollection();
    var ex = Assert.Throws<TechnicalException>(() => services.AddDatabaseOptions(null));
    Assert.Contains("No configuration", ex.Message);
  }
}