using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.MongoDb;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

public sealed class MongoContainerFixture : IAsyncLifetime
{
  private readonly MongoDbContainer _container =
    new MongoDbBuilder()
      .WithImage("mongo:7.0")
      .WithCleanUp(true)
      .Build();

  public string ConnectionString => _container.GetConnectionString();
  public string DatabaseName { get; } = $"franz-tests-{Guid.NewGuid():N}";

  public async Task InitializeAsync()
    => await _container.StartAsync();

  public async Task DisposeAsync()
    => await _container.DisposeAsync();
}

