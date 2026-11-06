using FluentAssertions;
using Franz.Common.Caching.Abstractions;

namespace Franz.Common.Caching.Tests.Abstractions;

public sealed class ICacheWriterTests
{
  [Fact]
  public void Interface_Should_Expose_SetAsync()
  {
    typeof(ICacheWriter).GetMethod("SetAsync").Should().NotBeNull();
  }
}
