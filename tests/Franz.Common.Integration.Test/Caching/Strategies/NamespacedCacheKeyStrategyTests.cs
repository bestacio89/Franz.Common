using Franz.Common.Caching.Estrategies;
using Xunit;
using FluentAssertions;

public class NamespacedCacheKeyStrategyTests
{
  private record TestReq(string A, int B);

  [Fact]
  public void BuildKey_Should_Include_Namespace_And_Request_Name()
  {
    var ns = new NamespacedCacheKeyStrategy("ns");
    var req = new TestReq("foo", 1);

    var key = ns.BuildKey(req);

    key.Should().StartWith("ns:");
    key.Should().Contain(nameof(TestReq));
  }

  [Fact]
  public void Different_Namespaces_Should_Produce_Different_Keys()
  {
    var ns1 = new NamespacedCacheKeyStrategy("alpha");
    var ns2 = new NamespacedCacheKeyStrategy("beta");
    var req = new TestReq("foo", 1);

    ns1.BuildKey(req).Should().NotBe(ns2.BuildKey(req));
  }
}
