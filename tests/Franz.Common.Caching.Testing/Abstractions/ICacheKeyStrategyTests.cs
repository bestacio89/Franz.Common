using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using System.Reflection;

namespace Franz.Common.Caching.Testing.Abstractions;





public sealed class ICacheKeyStrategyTests
{
  [Fact]
  public void Interface_Should_Define_GetKey_Method()
  {
    typeof(ICacheKeyStrategy)
      .GetMethod("GetKey")
      .Should()
      .NotBeNull();
  }

  [Fact]
  public void Interface_Should_Define_Generic_BuildKey_Method()
  {
    var method = typeof(ICacheKeyStrategy)
      .GetMethods()
      .Single(m => m.Name == "BuildKey");

    method.IsGenericMethod.Should().BeTrue();
  }
}
