using FluentAssertions;
using Franz.Common.Caching.Estrategies;

namespace Franz.Common.Caching.Tests.Strategies;

public sealed class NamespacedKeyStrategyTests
{
  [Fact]
  public void BuildKey_Should_UseDeterministicOpaqueKeyWithinNamespace()
  {
    const string sensitiveValue = "private-customer-payload";
    var strategy = new NamespacedCacheKeyStrategy("orders", new DefaultCacheKeyStrategy(TestJsonSerializerOptions.Create()));
    var request = new NamespacedRequest(42, sensitiveValue);

    var first = strategy.BuildKey(request);
    var second = strategy.BuildKey(request);

    first.Should().Be(second);
    first.Should().StartWith("orders:");
    first.Should().NotContain(sensitiveValue);
  }

  [Fact]
  public void GetKey_Should_UseTheSameUnderlyingStrategy()
  {
    var strategy = new NamespacedCacheKeyStrategy("orders", new DefaultCacheKeyStrategy(TestJsonSerializerOptions.Create()));
    var request = new NamespacedRequest(42, "payload");

    strategy.GetKey(request).Should().Be(strategy.BuildKey(request));
  }

  [Fact]
  public void Constructor_Should_RejectInvalidNamespace()
  {
    var action = () => new NamespacedCacheKeyStrategy(" ");

    action.Should().Throw<ArgumentException>();
  }

  private sealed record NamespacedRequest(int Id, string Value);
}
