using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Polly.Options;
using Franz.Common.Mediator.Polly.Pipelines;
using Franz.Common.Mediator.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Polly.Timeout;
using System;
using System.Net.Http;

namespace Franz.Common.Mediator.Polly
{
  /// <summary>
  /// Unified Franz Polly integration.
  /// Provides Mediator + HTTP resilience policies with a single registry and pipeline registration.
  /// </summary>
  public static class FranzPollyServiceCollectionExtensions
  {
    public static IServiceCollection AddFranzResilience(this IServiceCollection services, IConfiguration configuration)
    {
      var resilience = configuration.GetSection("Resilience");
      if (!resilience.Exists())
        return services;

      var registry = new PolicyRegistry();

      AddMediatorPolicies(services, registry, resilience);
      AddHttpPolicies(registry, resilience);

      services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);

      Console.WriteLine("🧭 Franz.PolicyRegistry initialized with:");
      foreach (var kvp in registry)
        Console.WriteLine($"   {kvp.Key}");

      return services;
    }

    // ==========================================================
    // MEDIATOR POLICIES (Untyped IAsyncPolicy)
    // ==========================================================
    private static void AddMediatorPolicies(IServiceCollection services, PolicyRegistry registry, IConfigurationSection section)
    {
      // --- Retry ---
      var retry = section.GetSection("RetryPolicy");
      if (retry.Exists() && retry.GetValue<bool>("Enabled"))
      {
        var count = retry.GetValue<int>("RetryCount", 3);
        var intervalMs = retry.GetValue<int>("RetryIntervalMilliseconds", 200);

        IAsyncPolicy mediatorRetry = Policy
          .Handle<Exception>()
          .WaitAndRetryAsync(count, _ => TimeSpan.FromMilliseconds(intervalMs));

        registry.Add("mediator:RetryPolicy", mediatorRetry);

        services.Configure<PollyRetryPipelineOptions>(o => o.PolicyName = "mediator:RetryPolicy");
        services.AddTransient(typeof(IPipeline<,>), typeof(PollyRetryPipeline<,>));
      }

      // --- Circuit Breaker ---
      var breaker = section.GetSection("CircuitBreaker");
      if (breaker.Exists() && breaker.GetValue<bool>("Enabled"))
      {
        var threshold = breaker.GetValue<double>("FailureThreshold", 0.5);
        var minThroughput = breaker.GetValue<int>("MinimumThroughput", 10);
        var duration = breaker.GetValue<int>("DurationOfBreakSeconds", 30);

        IAsyncPolicy mediatorBreaker = Policy
          .Handle<Exception>()
          .AdvancedCircuitBreakerAsync(
              failureThreshold: threshold,
              samplingDuration: TimeSpan.FromSeconds(30),
              minimumThroughput: minThroughput,
              durationOfBreak: TimeSpan.FromSeconds(duration));

        registry.Add("mediator:CircuitBreaker", mediatorBreaker);

        services.Configure<PollyCircuitBreakerPipelineOptions>(o => o.PolicyName = "mediator:CircuitBreaker");
        services.AddTransient(typeof(IPipeline<,>), typeof(PollyCircuitBreakerPipeline<,>));
      }

      // --- Timeout ---
      var timeout = section.GetSection("TimeoutPolicy");
      if (timeout.Exists() && timeout.GetValue<bool>("Enabled"))
      {
        var seconds = timeout.GetValue<int>("TimeoutSeconds", 5);

        IAsyncPolicy mediatorTimeout = Policy
          .TimeoutAsync(TimeSpan.FromSeconds(seconds), TimeoutStrategy.Optimistic);

        registry.Add("mediator:TimeoutPolicy", mediatorTimeout);

        services.Configure<PollyTimeoutPipelineOptions>(o => o.PolicyName = "mediator:TimeoutPolicy");
        services.AddTransient(typeof(IPipeline<,>), typeof(PollyTimeoutPipeline<,>));
      }

      // --- Bulkhead ---
      var bulkhead = section.GetSection("BulkheadPolicy");
      if (bulkhead.Exists() && bulkhead.GetValue<bool>("Enabled"))
      {
        var maxParallel = bulkhead.GetValue<int>("MaxParallelization", 10);
        var maxQueue = bulkhead.GetValue<int>("MaxQueueSize", 20);

        IAsyncPolicy mediatorBulkhead = Policy
          .BulkheadAsync(maxParallel, maxQueue);

        registry.Add("mediator:BulkheadPolicy", mediatorBulkhead);

        services.Configure<PollyBulkheadPipelineOptions>(o => o.PolicyName = "mediator:BulkheadPolicy");
        services.AddTransient(typeof(IPipeline<,>), typeof(PollyBulkheadPipeline<,>));
      }
    }

    // ==========================================================
    // HTTP POLICIES (Typed for HttpResponseMessage)
    // ==========================================================
    private static void AddHttpPolicies(PolicyRegistry registry, IConfigurationSection section)
    {
      // --- Retry ---
      var retry = section.GetSection("RetryPolicy");
      if (retry.Exists() && retry.GetValue<bool>("Enabled"))
      {
        var count = retry.GetValue<int>("RetryCount", 3);
        var intervalMs = retry.GetValue<int>("RetryIntervalMilliseconds", 200);

        var policy = Policy<HttpResponseMessage>
          .Handle<HttpRequestException>()
          .OrResult(r => !r.IsSuccessStatusCode)
          .WaitAndRetryAsync(count, _ => TimeSpan.FromMilliseconds(intervalMs));

        registry.Add("http:RetryPolicy", policy);
      }

      // --- Circuit Breaker ---
      var breaker = section.GetSection("CircuitBreaker");
      if (breaker.Exists() && breaker.GetValue<bool>("Enabled"))
      {
        var threshold = breaker.GetValue<double>("FailureThreshold", 0.5);
        var minThroughput = breaker.GetValue<int>("MinimumThroughput", 10);
        var duration = breaker.GetValue<int>("DurationOfBreakSeconds", 30);

        var policy = Policy<HttpResponseMessage>
          .Handle<HttpRequestException>()
          .OrResult(r => !r.IsSuccessStatusCode)
          .AdvancedCircuitBreakerAsync(
            threshold,
            TimeSpan.FromSeconds(30),
            minThroughput,
            TimeSpan.FromSeconds(duration));

        registry.Add("http:CircuitBreaker", policy);
      }

      // --- Timeout ---
      var timeout = section.GetSection("TimeoutPolicy");
      if (timeout.Exists() && timeout.GetValue<bool>("Enabled"))
      {
        var seconds = timeout.GetValue<int>("TimeoutSeconds", 5);

        var policy = Policy.TimeoutAsync<HttpResponseMessage>(
          TimeSpan.FromSeconds(seconds),
          TimeoutStrategy.Optimistic);

        registry.Add("http:TimeoutPolicy", policy);
      }

      // --- Bulkhead ---
      var bulkhead = section.GetSection("BulkheadPolicy");
      if (bulkhead.Exists() && bulkhead.GetValue<bool>("Enabled"))
      {
        var maxParallel = bulkhead.GetValue<int>("MaxParallelization", 10);
        var maxQueue = bulkhead.GetValue<int>("MaxQueueSize", 20);

        var policy = Policy.BulkheadAsync<HttpResponseMessage>(maxParallel, maxQueue);
        registry.Add("http:BulkheadPolicy", policy);
      }
    }
  }
}
