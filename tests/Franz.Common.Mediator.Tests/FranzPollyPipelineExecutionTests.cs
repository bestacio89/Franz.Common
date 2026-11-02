using Franz.Common.Mediator.Polly;
using Franz.Common.Mediator.Polly.Options;
using Franz.Common.Mediator.Polly.Pipelines;
using Franz.Common.Mediator.Polly.Observers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;

namespace Franz.Common.Mediator.Tests;

public sealed class FranzPollyPipelineExecutionTests
{
  private sealed record TestRequest;

  private static PolicyRegistry CreateRegistry(string policyName, IAsyncPolicy policy)
  {
    var registry = new PolicyRegistry();
    registry.Add(policyName, policy);
    return registry;
  }

  [Theory]
  [InlineData("retry")]
  [InlineData("circuit-breaker")]
  [InlineData("advanced-circuit-breaker")]
  [InlineData("timeout")]
  [InlineData("bulkhead")]
  public async Task Untyped_policy_executes_downstream_handler_exactly_once(string pipeline)
  {
    const string policyName = "test-policy";
    var registry = CreateRegistry(policyName, Policy.NoOpAsync());
    var handlerInvocations = 0;
    var request = new TestRequest();
    Func<Task<string>> next = () =>
    {
      handlerInvocations++;
      return Task.FromResult("completed");
    };

    var result = pipeline switch
    {
      "retry" => await new PollyRetryPipeline<TestRequest, string>(
          registry,
          Options.Create(new PollyRetryPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "circuit-breaker" => await new PollyCircuitBreakerPipeline<TestRequest, string>(
          registry,
          Options.Create(new PollyCircuitBreakerPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "advanced-circuit-breaker" => await new PollyAdvancedCircuitBreakerPipeline<TestRequest, string>(
          registry,
          Options.Create(new PollyCircuitBreakerPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "timeout" => await new PollyTimeoutPipeline<TestRequest, string>(
          registry,
          Options.Create(new PollyTimeoutPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "bulkhead" => await new PollyBulkheadPipeline<TestRequest, string>(
          registry,
          Options.Create(new PollyBulkheadPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      _ => throw new ArgumentOutOfRangeException(nameof(pipeline))
    };

    Assert.Equal("completed", result);
    Assert.Equal(1, handlerInvocations);
  }
}
