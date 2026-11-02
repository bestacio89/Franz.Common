using Franz.Common.Mediator.Polly;
using Franz.Common.Mediator.Pipelines.Core;
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
          Microsoft.Extensions.Options.Options.Create(new PollyRetryPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "circuit-breaker" => await new PollyCircuitBreakerPipeline<TestRequest, string>(
          registry,
          Microsoft.Extensions.Options.Options.Create(new PollyCircuitBreakerPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "advanced-circuit-breaker" => await new PollyAdvancedCircuitBreakerPipeline<TestRequest, string>(
          registry,
          Microsoft.Extensions.Options.Options.Create(new PollyCircuitBreakerPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "timeout" => await new PollyTimeoutPipeline<TestRequest, string>(
          registry,
          Microsoft.Extensions.Options.Options.Create(new PollyTimeoutPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "bulkhead" => await new PollyBulkheadPipeline<TestRequest, string>(
          registry,
          Microsoft.Extensions.Options.Options.Create(new PollyBulkheadPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      _ => throw new ArgumentOutOfRangeException(nameof(pipeline))
    };

    Assert.Equal("completed", result);
    Assert.Equal(1, handlerInvocations);
  }
  [Fact]
  public async Task Retry_policy_reinvokes_downstream_only_for_a_failed_attempt()
  {
    const string policyName = "retry-policy";
    var registry = CreateRegistry(
        policyName,
        Policy.Handle<InvalidOperationException>().RetryAsync(1));
    var handlerInvocations = 0;
    var pipeline = new PollyRetryPipeline<TestRequest, string>(
        registry,
        Microsoft.Extensions.Options.Options.Create(new PollyRetryPipelineOptions { PolicyName = policyName }),
        NullLogger<TestRequest>.Instance,
        Array.Empty<IResilienceObserver>());

    var result = await pipeline.Handle(new TestRequest(), () =>
    {
      handlerInvocations++;
      if (handlerInvocations == 1)
        throw new InvalidOperationException("Transient failure.");

      return Task.FromResult("completed");
    });

    Assert.Equal("completed", result);
    Assert.Equal(2, handlerInvocations);
  }

  [Theory]
  [InlineData("retry")]
  [InlineData("circuit-breaker")]
  [InlineData("advanced-circuit-breaker")]
  [InlineData("timeout")]
  [InlineData("bulkhead")]
  [InlineData("composite")]
  public async Task Cancelled_policy_does_not_invoke_downstream_handler(string pipelineName)
  {
    const string policyName = "cancelled-policy";
    var registry = CreateRegistry(policyName, Policy.NoOpAsync());
    registry.Add("mediator:Default", Policy.NoOpAsync());
    var pipeline = CreatePipeline(pipelineName, registry, policyName);
    using var cancellation = new CancellationTokenSource();
    await cancellation.CancelAsync();
    var handlerInvocations = 0;

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
        pipeline.Handle(new TestRequest(), () =>
        {
          handlerInvocations++;
          return Task.FromResult("unexpected");
        }, cancellation.Token));

    Assert.Equal(0, handlerInvocations);
  }

  private static IPipeline<TestRequest, string> CreatePipeline(
      string pipelineName,
      PolicyRegistry registry,
      string policyName)
  {
    var observers = Array.Empty<IResilienceObserver>();
    var logger = NullLogger<TestRequest>.Instance;

    return pipelineName switch
    {
      "retry" => new PollyRetryPipeline<TestRequest, string>(
          registry, global::Microsoft.Extensions.Options.Options.Create(new PollyRetryPipelineOptions { PolicyName = policyName }), logger, observers),
      "circuit-breaker" => new PollyCircuitBreakerPipeline<TestRequest, string>(
          registry, global::Microsoft.Extensions.Options.Options.Create(new PollyCircuitBreakerPipelineOptions { PolicyName = policyName }), logger, observers),
      "advanced-circuit-breaker" => new PollyAdvancedCircuitBreakerPipeline<TestRequest, string>(
          registry, global::Microsoft.Extensions.Options.Options.Create(new PollyCircuitBreakerPipelineOptions { PolicyName = policyName }), logger, observers),
      "timeout" => new PollyTimeoutPipeline<TestRequest, string>(
          registry, global::Microsoft.Extensions.Options.Options.Create(new PollyTimeoutPipelineOptions { PolicyName = policyName }), logger, observers),
      "bulkhead" => new PollyBulkheadPipeline<TestRequest, string>(
          registry, global::Microsoft.Extensions.Options.Options.Create(new PollyBulkheadPipelineOptions { PolicyName = policyName }), logger, observers),
      "composite" => new PollyResiliencePipeline<TestRequest, string>(
          registry, logger, observers),
      _ => throw new ArgumentOutOfRangeException(nameof(pipelineName))
    };
  }

  private sealed class RecordingObserver : IResilienceObserver
  {
    public Franz.Common.Mediator.Polly.Context.ResilienceContext? LastContext { get; private set; }

    public void OnPolicyExecuted(
        string policyName,
        Franz.Common.Mediator.Polly.Context.ResilienceContext context)
    {
      LastContext = context;
    }
  }

  [Fact]
  public async Task Retry_policy_counts_result_based_retries()
  {
    const string policyName = "result-retry-policy";
    var registry = new PolicyRegistry();
    registry.Add(
        policyName,
        Policy<string>.HandleResult(static result => result == "retry").RetryAsync(1));
    var observer = new RecordingObserver();
    var pipeline = new PollyRetryPipeline<TestRequest, string>(
        registry,
        Microsoft.Extensions.Options.Options.Create(new PollyRetryPipelineOptions { PolicyName = policyName }),
        NullLogger<TestRequest>.Instance,
        [observer]);
    var handlerInvocations = 0;

    var result = await pipeline.Handle(new TestRequest(), () =>
    {
      handlerInvocations++;
      return Task.FromResult(handlerInvocations == 1 ? "retry" : "completed");
    });

    Assert.Equal("completed", result);
    Assert.Equal(2, handlerInvocations);
    Assert.NotNull(observer.LastContext);
    Assert.Equal(1, observer.LastContext.RetryCount);
  }

  private sealed class ThrowingObserver : IResilienceObserver
  {
    public void OnPolicyExecuted(
        string policyName,
        Franz.Common.Mediator.Polly.Context.ResilienceContext context)
    {
      throw new InvalidOperationException("Telemetry sink failure.");
    }
  }

  [Fact]
  public async Task Observer_failure_does_not_change_successful_policy_outcome()
  {
    const string policyName = "observer-policy";
    var registry = CreateRegistry(policyName, Policy.NoOpAsync());
    var pipeline = new PollyRetryPipeline<TestRequest, string>(
        registry,
        Microsoft.Extensions.Options.Options.Create(new PollyRetryPipelineOptions { PolicyName = policyName }),
        NullLogger<TestRequest>.Instance,
        [new ThrowingObserver()]);
    var handlerInvocations = 0;

    var result = await pipeline.Handle(new TestRequest(), () =>
    {
      handlerInvocations++;
      return Task.FromResult("completed");
    });

    Assert.Equal("completed", result);
    Assert.Equal(1, handlerInvocations);
  }

  [Theory]
  [InlineData("retry")]
  [InlineData("circuit-breaker")]
  [InlineData("advanced-circuit-breaker")]
  [InlineData("timeout")]
  [InlineData("bulkhead")]
  public async Task Typed_policy_executes_downstream_handler_exactly_once(string pipeline)
  {
    const string policyName = "typed-test-policy";
    var registry = new PolicyRegistry();
    registry.Add(policyName, Policy.NoOpAsync<string>());
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
          Microsoft.Extensions.Options.Options.Create(new PollyRetryPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "circuit-breaker" => await new PollyCircuitBreakerPipeline<TestRequest, string>(
          registry,
          Microsoft.Extensions.Options.Options.Create(new PollyCircuitBreakerPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "advanced-circuit-breaker" => await new PollyAdvancedCircuitBreakerPipeline<TestRequest, string>(
          registry,
          Microsoft.Extensions.Options.Options.Create(new PollyCircuitBreakerPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "timeout" => await new PollyTimeoutPipeline<TestRequest, string>(
          registry,
          Microsoft.Extensions.Options.Options.Create(new PollyTimeoutPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      "bulkhead" => await new PollyBulkheadPipeline<TestRequest, string>(
          registry,
          Microsoft.Extensions.Options.Options.Create(new PollyBulkheadPipelineOptions { PolicyName = policyName }),
          NullLogger<TestRequest>.Instance,
          Array.Empty<IResilienceObserver>()).Handle(request, next),
      _ => throw new ArgumentOutOfRangeException(nameof(pipeline))
    };

    Assert.Equal("completed", result);
    Assert.Equal(1, handlerInvocations);
  }

}
