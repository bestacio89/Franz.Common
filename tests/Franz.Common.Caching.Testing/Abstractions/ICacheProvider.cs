using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using System;
using Xunit;

namespace Franz.Common.Caching.Testing.Abstractions;

public sealed class ICacheProviderTests
{
  [Fact]
  public void Interface_Should_Expose_GetOrSetAsync()
  {
    typeof(ICacheProvider).GetMethod("GetOrSetAsync")
        .Should().NotBeNull();
  }

  [Fact]
  public void Interface_Should_Expose_RemoveAsync()
  {
    typeof(ICacheProvider).GetMethod("RemoveAsync")
        .Should().NotBeNull();
  }

  [Fact]
  public void Interface_Should_Expose_RemoveByTagAsync()
  {
    typeof(ICacheProvider).GetMethod("RemoveByTagAsync")
        .Should().NotBeNull();
  }
}
