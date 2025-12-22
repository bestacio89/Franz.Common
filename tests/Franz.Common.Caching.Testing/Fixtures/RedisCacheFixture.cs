using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.Redis;

namespace Franz.Common.Caching.Testing.Fixtures;

public sealed class RedisCacheFixture
  : IAsyncLifetime
{
  private readonly RedisContainer _container;

  public string ConnectionString => _container.GetConnectionString();

  public RedisCacheFixture()
  {
    _container = new RedisBuilder()
      .WithImage("redis:7.2-alpine")
      .WithCleanUp(true)
      .Build();
  }

  public async Task InitializeAsync()
    => await _container.StartAsync();

  public async Task DisposeAsync()
    => await _container.DisposeAsync();
}
