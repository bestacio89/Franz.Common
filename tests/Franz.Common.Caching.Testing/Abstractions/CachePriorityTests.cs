using FluentAssertions;
using Franz.Common.Caching.Abstractions;


namespace Franz.Common.Caching.Testing.Abstractions;

public sealed class CachePriorityTests
{
  [Fact]
  public void CachePriority_Should_Have_Expected_Numeric_Values()
  {
    ((int)CachePriority.Low).Should().Be(0);
    ((int)CachePriority.Normal).Should().Be(1);
    ((int)CachePriority.High).Should().Be(2);
  }

  [Fact]
  public void CachePriority_Should_Define_Three_Values()
  {
    Enum.GetValues<CachePriority>().Length.Should().Be(3);
  }
}

