using FluentAssertions;
using Franz.Common.Caching.Estrategies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Strategies;

public sealed class NamespacedCacheKeyStrategyTests
{
  private sealed record TestRequest(int Id);

  [Fact]
  public void BuildKey_Should_Prefix_With_Namespace()
  {
    var strategy = new NamespacedCacheKeyStrategy("franz");

    var key = strategy.BuildKey(new TestRequest(1));

    key.Should().StartWith("franz:");
  }

  [Fact]
  public void BuildKey_Should_Isolate_By_Namespace()
  {
    var request = new TestRequest(1);

    var key1 = new NamespacedCacheKeyStrategy("ns1").BuildKey(request);
    var key2 = new NamespacedCacheKeyStrategy("ns2").BuildKey(request);

    key1.Should().NotBe(key2);
  }

  [Fact]
  public void BuildKey_Should_Be_Deterministic()
  {
    var strategy = new NamespacedCacheKeyStrategy("franz");
    var request = new TestRequest(1);

    strategy.BuildKey(request)
            .Should()
            .Be(strategy.BuildKey(request));
  }
}