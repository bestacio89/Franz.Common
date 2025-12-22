using FluentAssertions;
using Franz.Common.Caching.Estrategies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Strategies;

public sealed class DefaultCacheKeyStrategyTests
{
  private sealed record TestRequest(int Id, string Name);

  [Fact]
  public void GetKey_Should_Include_Type_Name()
  {
    var strategy = new DefaultCacheKeyStrategy();
    var key = strategy.GetKey(new TestRequest(1, "a"));

    key.Should().Contain(nameof(TestRequest));
    key.Should().StartWith("mediator:");
  }

  [Fact]
  public void BuildKey_Should_Be_Deterministic()
  {
    var strategy = new DefaultCacheKeyStrategy();
    var request = new TestRequest(1, "a");

    strategy.BuildKey(request)
            .Should()
            .Be(strategy.BuildKey(request));
  }

  [Fact]
  public void BuildKey_Should_Change_When_Request_Changes()
  {
    var strategy = new DefaultCacheKeyStrategy();

    var key1 = strategy.BuildKey(new TestRequest(1, "a"));
    var key2 = strategy.BuildKey(new TestRequest(2, "a"));

    key1.Should().NotBe(key2);
  }
}