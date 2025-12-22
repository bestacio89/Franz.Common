using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Abstractions;

public sealed class CacheEntryOptionsTests
{
  [Fact]
  public void Constructor_Should_Set_Default_Values()
  {
    var options = new CacheEntryOptions();

    options.Ttl.Should().Be(TimeSpan.FromMinutes(5));
    options.Sliding.Should().BeFalse();
    options.Priority.Should().Be(CachePriority.Normal);
  }

  [Fact]
  public void Properties_Should_Be_Settable()
  {
    var options = new CacheEntryOptions
    {
      Ttl = TimeSpan.FromSeconds(30),
      Sliding = true,
      Priority = CachePriority.High
    };

    options.Ttl.Should().Be(TimeSpan.FromSeconds(30));
    options.Sliding.Should().BeTrue();
    options.Priority.Should().Be(CachePriority.High);
  }
}
