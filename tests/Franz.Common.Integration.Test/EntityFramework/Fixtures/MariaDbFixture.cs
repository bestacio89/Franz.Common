using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.MariaDb;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework.Fixtures;

public class MariaDbFixture : IAsyncLifetime
{
  public MariaDbContainer Container { get; } = new MariaDbContainer(new MariaDbConfiguration("mariadb:11.4"))
  {
    // Custom configurations if needed, but the builder is usually preferred 
    // for properties like Database/Username. 
    // Using the Builder with the image string in the constructor:
  };

  // Note: In the latest Testcontainers versions, the pattern is:
  public MariaDbContainer MariaDbContainer { get; } = new MariaDbBuilder("mariadb:11.4")
      .WithDatabase("library")
      .WithUsername("root")
      .WithPassword("password")
      .Build();

  public async Task InitializeAsync() => await MariaDbContainer.StartAsync();
  public async Task DisposeAsync() => await MariaDbContainer.DisposeAsync();
}