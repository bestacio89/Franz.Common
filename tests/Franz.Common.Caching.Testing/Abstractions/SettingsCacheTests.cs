using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Abstractions;


public sealed class ISettingsCacheTests
{
  [Fact]
  public void Interface_Should_Expose_GetSettingAsync()
  {
    typeof(ISettingsCache).GetMethod("GetSettingAsync")
      .Should().NotBeNull();
  }

  [Fact]
  public void Interface_Should_Expose_SetSettingAsync()
  {
    typeof(ISettingsCache).GetMethod("SetSettingAsync")
      .Should().NotBeNull();
  }
}
