using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Caching.Testing.Extensions;

using FluentAssertions;
using Franz.Common.Caching.Extensions;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Caching.Testing.Models;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.DependencyInjection;

public sealed class AddFranzMediatorCachingTests
{
  [Fact]
  public void Should_Register_CachingPipeline()
  {
    using var sp = ServiceTestHelper.Build(services =>
      services.AddFranzMediatorCaching());

    var pipeline = sp.GetServices(typeof(IPipeline<,>));

    pipeline.Should().ContainSingle(p =>
      p.GetType().GetGenericTypeDefinition() == typeof(CachingPipeline<,>));
  }
}
