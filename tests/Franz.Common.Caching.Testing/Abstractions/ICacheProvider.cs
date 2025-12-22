using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Abstractions;

public sealed class ICacheProviderTests
{
  [Fact]
  public void Interface_Should_Expose_GetAsync()
  {
    typeof(ICacheProvider).GetMethod("GetAsync")
      .Should().NotBeNull();
  }

  [Fact]
  public void Interface_Should_Expose_SetAsync()
  {
    typeof(ICacheProvider).GetMethod("SetAsync")
      .Should().NotBeNull();
  }

  [Fact]
  public void Interface_Should_Expose_RemoveAsync()
  {
    typeof(ICacheProvider).GetMethod("RemoveAsync")
      .Should().NotBeNull();
  }

  [Fact]
  public void Interface_Should_Expose_ExistsAsync()
  {
    typeof(ICacheProvider).GetMethod("ExistsAsync")
      .Should().NotBeNull();
  }
}