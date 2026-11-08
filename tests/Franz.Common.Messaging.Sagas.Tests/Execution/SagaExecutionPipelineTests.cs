using FluentAssertions;
using Franz.Common.Messaging.Sagas.Core;

namespace Franz.Common.Messaging.Sagas.Tests.Execution;

public sealed class SagaExecutionPipelineTests
{
  [Fact]
  public async Task ExecuteAsync_ExecutesMiddlewareInConfiguredOuterToInnerOrder()
  {
    var pipeline = new SagaExecutionPipeline();
    var calls = new List<string>();

    pipeline.Use(async next => { calls.Add("outer-before"); await next(); calls.Add("outer-after"); });
    pipeline.Use(async next => { calls.Add("inner-before"); await next(); calls.Add("inner-after"); });

    await pipeline.ExecuteAsync(() => { calls.Add("handler"); return Task.CompletedTask; });

    calls.Should().Equal("outer-before", "inner-before", "handler", "inner-after", "outer-after");
  }

  [Fact]
  public async Task ExecuteAsync_PropagatesHandlerFailuresThroughMiddleware()
  {
    var pipeline = new SagaExecutionPipeline();
    var middlewareCalled = false;
    pipeline.Use(async next => { middlewareCalled = true; await next(); });

    var act = () => pipeline.ExecuteAsync(
        () => Task.FromException(new InvalidOperationException("handler failed")));

    await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failed");
    middlewareCalled.Should().BeTrue();
  }
}
