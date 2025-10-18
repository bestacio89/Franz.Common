using Franz.Common.Caching.Estrategies;
using Xunit;
using FluentAssertions;

public class DefaultCacheKeyStrategyTests
{
  private record TestRequest(string Name, int Value);

  [Fact]
  public void BuildKey_Should_Include_Request_Type_And_Serialized_Content()
  {
    var strategy = new DefaultCacheKeyStrategy();
    var request = new TestRequest("Alpha", 42);

    var key = strategy.BuildKey(request);

    key.Should().Contain(nameof(TestRequest));
    key.Should().Contain("Alpha");
  }

  [Fact]
  public void Different_Requests_Should_Produce_Different_Keys()
  {
    var strategy = new DefaultCacheKeyStrategy();
    var key1 = strategy.BuildKey(new TestRequest("A", 1));
    var key2 = strategy.BuildKey(new TestRequest("B", 1));

    key1.Should().NotBe(key2);
  }
}
