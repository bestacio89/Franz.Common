using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Polly.Options;
using Franz.Common.Mediator.Polly.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly.Registry;

namespace Franz.Common.Mediator.Polly
{
  public static class FranzPollyServiceCollectionExtensions
  {

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
    public static IServiceCollection AddFranzPollyRetry(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyRetryPipelineOptions>>(o =>
      {
        o.PolicyName = policyName;
      });
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyRetryPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzPollyCircuitBreaker(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyCircuitBreakerPipelineOptions>>(o =>
      {
        o.PolicyName = policyName;
      });
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyCircuitBreakerPipeline<,>));
      return services;
    }

    public static IServiceCollection AddFranzPollyTimeout(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyTimeoutPipelineOptions>>(o =>
      {
        o.PolicyName = policyName;
      });
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyTimeoutPipeline<,>));
      return services;
    }
     public static IServiceCollection AddFranzPollyAdvancedCircuitBreaker(
        this IServiceCollection services,
        string policyName)
    { services.Configure<PollyPipelineOptions<PollyAdvancedCircuitBreakerOptions>>(o =>
      {
        o.PolicyName = policyName;
      });
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyAdvancedCircuitBreakerPipeline<,>));
      return services;
    }
    public static IServiceCollection AddFranzPollyBulkhead(
        this IServiceCollection services,
        string policyName)
    {
      services.Configure<PollyPipelineOptions<PollyBulkheadPipelineOptions>>(o =>
      {
        o.PolicyName = policyName;
      });
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyBulkheadPipeline<,>));
      return services;
    }
  }

   
  // simple generic wrapper for options
  public class PollyPipelineOptions<TOptions>
  {
    public string PolicyName { get; set; } = string.Empty;
  }
}
