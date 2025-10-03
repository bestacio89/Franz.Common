using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Polly.Options;
using Franz.Common.Mediator.Polly.Pipelines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;

namespace Franz.Common.Mediator.Polly
{
  public static class FranzPollyServiceCollectionExtensions
  {
    // ------------------------
    // Core Policy Registry
    // ------------------------
    public static IServiceCollection AddFranzPollyPolicies(
        this IServiceCollection services,
        Action<PollyPolicyRegistryOptions> configure)
    {
      var options = new PollyPolicyRegistryOptions();
      configure(options);

      services.AddSingleton<IReadOnlyPolicyRegistry<string>>(sp =>
      {
        var registry = new PolicyRegistry();
        foreach (var policy in options.Policies)
        {
          registry.Add(policy.Key, policy.Value);
        }
        return registry;
      });

      return services;
    }

    // ------------------------
    // Mediator Pipelines
    // ------------------------
    public static IServiceCollection AddFranzPollyRetry(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyRetryPipelineOptions>>(o => o.PolicyName = policyName);
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyRetryPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzPollyCircuitBreaker(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyCircuitBreakerPipelineOptions>>(o => o.PolicyName = policyName);
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyCircuitBreakerPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzPollyTimeout(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyTimeoutPipelineOptions>>(o => o.PolicyName = policyName);
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyTimeoutPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzPollyAdvancedCircuitBreaker(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyAdvancedCircuitBreakerOptions>>(o => o.PolicyName = policyName);
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyAdvancedCircuitBreakerPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzPollyBulkhead(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyBulkheadPipelineOptions>>(o => o.PolicyName = policyName);
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyBulkheadPipeline<,>));
      return services;
    }

    // ------------------------
    // Unified Config-Driven Entry Point
    // ------------------------
    public static IServiceCollection AddFranzResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
      var section = configuration.GetSection("Resilience");

      services.AddFranzPollyPolicies(options =>
      {
        if (section.GetSection("RetryPolicy").GetValue<bool>("Enabled"))
        {
          var count = section.GetSection("RetryPolicy").GetValue<int>("RetryCount");
          var interval = section.GetSection("RetryPolicy").GetValue<int>("RetryIntervalMilliseconds");

          options.AddRetry("RetryPolicy", count, interval);
          services.AddFranzPollyRetry("RetryPolicy");
        }

        if (section.GetSection("CircuitBreaker").GetValue<bool>("Enabled"))
        {
          var threshold = section.GetSection("CircuitBreaker").GetValue<double>("FailureThreshold");
          var throughput = section.GetSection("CircuitBreaker").GetValue<int>("MinimumThroughput");
          var duration = section.GetSection("CircuitBreaker").GetValue<int>("DurationOfBreakSeconds");

          options.AddCircuitBreaker("CircuitBreaker", threshold, throughput, duration);
          services.AddFranzPollyCircuitBreaker("CircuitBreaker");
        }

        if (section.GetSection("TimeoutPolicy").GetValue<bool>("Enabled"))
        {
          var timeout = section.GetSection("TimeoutPolicy").GetValue<int>("TimeoutSeconds");

          options.AddTimeout("TimeoutPolicy", timeout);
          services.AddFranzPollyTimeout("TimeoutPolicy");
        }

        if (section.GetSection("BulkheadPolicy").GetValue<bool>("Enabled"))
        {
          var maxParallel = section.GetSection("BulkheadPolicy").GetValue<int>("MaxParallelization");
          var maxQueue = section.GetSection("BulkheadPolicy").GetValue<int>("MaxQueueSize");

          options.AddBulkhead("BulkheadPolicy", maxParallel, maxQueue);
          services.AddFranzPollyBulkhead("BulkheadPolicy");
        }
      });

      return services;
    }
  }

  // ------------------------
  // Options Wrappers
  // ------------------------
  public class PollyPipelineOptions<TOptions>
  {
    public string PolicyName { get; set; } = string.Empty;
  }

  // ------------------------
  // Policy Registry Builder
  // ------------------------
  public static class PollyPolicyRegistryOptionsExtensions
  {
    public static void AddRetry(this PollyPolicyRegistryOptions options, string name, int retryCount, int intervalMs)
    {
      var policy = Policy<HttpResponseMessage>.Handle<Exception>()
        .WaitAndRetryAsync(retryCount, _ => TimeSpan.FromMilliseconds(intervalMs));
      options.Policies.Add(name, policy);
    }

    public static void AddCircuitBreaker(this PollyPolicyRegistryOptions options, string name,
      double failureThreshold, int minimumThroughput, int durationSeconds)
    {
      var policy = Policy<HttpResponseMessage>.Handle<Exception>()
        .AdvancedCircuitBreakerAsync(
          failureThreshold: failureThreshold,
          samplingDuration: TimeSpan.FromSeconds(30),
          minimumThroughput: minimumThroughput,
          durationOfBreak: TimeSpan.FromSeconds(durationSeconds));
      options.Policies.Add(name, policy);
    }

    public static void AddTimeout(this PollyPolicyRegistryOptions options, string name, int timeoutSeconds)
    {
      var policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeoutSeconds));
      options.Policies.Add(name, policy);
    }

    public static void AddBulkhead(this PollyPolicyRegistryOptions options, string name, int maxParallelization, int maxQueueSize)
    {
      var policy = Policy.BulkheadAsync<HttpResponseMessage>(maxParallelization, maxQueueSize);
      options.Policies.Add(name, policy);
    }
  }
}
