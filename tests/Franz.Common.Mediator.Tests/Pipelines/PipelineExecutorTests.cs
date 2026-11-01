using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Pipelines.Core;
using Xunit;

namespace Franz.Common.Mediator.Tests.Pipelines;

public sealed class PipelineExecutorTests
{
  private sealed record TestRequest;

  private sealed class ReentrantPipeline : IPipeline<TestRequest, string>
  {
    public int InvocationCount { get; private set; }

    public async Task<string> Handle(
        TestRequest request,
        Func<Task<string>> next,
        CancellationToken cancellationToken = default)
    {
      InvocationCount++;
      await next().ConfigureAwait(false);
      return await next().ConfigureAwait(false);
    }
  }

  private sealed class CountingPipeline : IPipeline<TestRequest, string>
  {
    public int InvocationCount { get; private set; }

    public async Task<string> Handle(
        TestRequest request,
        Func<Task<string>> next,
        CancellationToken cancellationToken = default)
    {
      InvocationCount++;
      return await next().ConfigureAwait(false);
    }
  }

  private sealed class ReentrantVoidPipeline : IPipeline<TestRequest, Unit>
  {
    public async Task<Unit> Handle(
        TestRequest request,
        Func<Task<Unit>> next,
        CancellationToken cancellationToken = default)
    {
      await next().ConfigureAwait(false);
      return await next().ConfigureAwait(false);
    }
  }

  private sealed class CountingVoidPipeline : IPipeline<TestRequest, Unit>
  {
    public int InvocationCount { get; private set; }

    public async Task<Unit> Handle(
        TestRequest request,
        Func<Task<Unit>> next,
        CancellationToken cancellationToken = default)
    {
      InvocationCount++;
      await next().ConfigureAwait(false);
      return Unit.Value;
    }
  }

  [Fact]
  public async Task ExecuteAsync_ValuePipeline_ReinvokesTheEntireDownstreamChainWhenNextIsCalledAgain()
  {
    var retryPipeline = new ReentrantPipeline();
    var downstreamPipeline = new CountingPipeline();
    var handlerInvocationCount = 0;
    IPipeline<TestRequest, string>[] pipelines = [retryPipeline, downstreamPipeline];

    var result = await PipelineExecutor.ExecuteAsync(
        new TestRequest(),
        pipelines,
        (_, _) =>
        {
          handlerInvocationCount++;
          return Task.FromResult("completed");
        });

    Assert.Equal("completed", result);
    Assert.Equal(1, retryPipeline.InvocationCount);
    Assert.Equal(2, downstreamPipeline.InvocationCount);
    Assert.Equal(2, handlerInvocationCount);
  }

  [Fact]
  public async Task ExecuteAsync_VoidPipeline_ReinvokesTheEntireDownstreamChainWhenNextIsCalledAgain()
  {
    var retryPipeline = new ReentrantVoidPipeline();
    var downstreamPipeline = new CountingVoidPipeline();
    var handlerInvocationCount = 0;
    IPipeline<TestRequest, Unit>[] pipelines = [retryPipeline, downstreamPipeline];

    await PipelineExecutor.ExecuteAsync(
        new TestRequest(),
        pipelines,
        (_, _) =>
        {
          handlerInvocationCount++;
          return Task.CompletedTask;
        });

    Assert.Equal(2, downstreamPipeline.InvocationCount);
    Assert.Equal(2, handlerInvocationCount);
  }
}
