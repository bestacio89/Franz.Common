using DotNet.Testcontainers.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.MariaDb;
using Xunit;

namespace Franz.Common.Integration.Tests.EntityFramework.Fixtures;

public class MariaDbFixture : IAsyncLifetime
{
  // Fix: Move the image string into the constructor to avoid the obsolete warning
  // and ensure the builder is correctly initialized for the AzDO environment.
  public MariaDbContainer Container { get; } =
      new MariaDbBuilder("mariadb:11.4")
          .WithDatabase("library")
          .WithUsername("root")
          .WithPassword("password")
          .WithWaitStrategy(
              Wait.ForUnixContainer()
                  .UntilInternalTcpPortIsAvailable (3306)
                  .UntilMessageIsLogged("ready for connections"))
          .Build();

  public async Task InitializeAsync() => await Container.StartAsync();

  public async Task DisposeAsync() => await Container.DisposeAsync();
}