using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Providers;
using Franz.Common.Caching.Testing.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Extensions;

public sealed class AddFranzRedisCachingTests
{
  [Fact]
  public void Should_Register_Redis_Provider()
  {
    using var sp = ServiceTestHelper.Build(services =>
      services.AddFranzRedisCaching("localhost:6379"));

    sp.GetRequiredService<IConnectionMultiplexer>()
      .Should().NotBeNull();

    sp.GetRequiredService<ICacheProvider>()
      .GetType().Name.Should().Be("RedisCacheProvider");
  }
  [Fact]
  public void Redis_Factory_Overload_Should_Be_Used()
  {
    var muxer = new Mock<IConnectionMultiplexer>().Object;

    using var sp = ServiceTestHelper.Build(services =>
      services.AddFranzRedisCaching(_ => muxer));

    sp.GetRequiredService<IConnectionMultiplexer>()
      .Should().BeSameAs(muxer);

    sp.GetRequiredService<ICacheProvider>()
      .Should().BeOfType<RedisCacheProvider>();
  }

  [Fact]
  public void AddFranzCaching_Should_Default_To_Memory()
  {
    using var sp = ServiceTestHelper.Build(services =>
      services.AddFranzCaching());

    sp.GetRequiredService<ICacheProvider>()
      .Should().BeOfType<MemoryCacheProvider>();
  }


}