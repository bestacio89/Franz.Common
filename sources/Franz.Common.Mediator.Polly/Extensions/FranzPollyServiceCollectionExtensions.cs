using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Polly.Timeout;
using System;
using System.Net.Http;

namespace Franz.Common.Mediator.Polly;

public static class FranzPollyServiceCollectionExtensions
{
  public static IServiceCollection AddFranzResilience(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);

    var resilience = configuration.GetSection("Resilience");

    if (!resilience.Exists())
      return services;

    var registry = new PolicyRegistry();

    AddHttpPolicies(registry, resilience);

    services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);

    return services;
  }

  private static void AddHttpPolicies(
      PolicyRegistry registry,
      IConfigurationSection section)
  {
    RegisterRetryPolicy(registry, section);
    RegisterCircuitBreakerPolicy(registry, section);
    RegisterTimeoutPolicy(registry, section);
    RegisterBulkheadPolicy(registry, section);
  }

  private static void RegisterRetryPolicy(
      PolicyRegistry registry,
      IConfigurationSection section)
  {
    var retry = section.GetSection("RetryPolicy");

    if (!retry.Exists() || !retry.GetValue<bool>("Enabled"))
      return;

    var retryCount = retry.GetValue("RetryCount", 3);
    var retryInterval = retry.GetValue("RetryIntervalMilliseconds", 200);

    var policy =
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(static response => !response.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount,
                _ => TimeSpan.FromMilliseconds(retryInterval));

    registry.Add("http:RetryPolicy", policy);
  }

  private static void RegisterCircuitBreakerPolicy(
      PolicyRegistry registry,
      IConfigurationSection section)
  {
    var breaker = section.GetSection("CircuitBreaker");

    if (!breaker.Exists() || !breaker.GetValue<bool>("Enabled"))
      return;

    var threshold = breaker.GetValue("FailureThreshold", 0.5);
    var minimumThroughput = breaker.GetValue("MinimumThroughput", 10);
    var breakDuration = breaker.GetValue("DurationOfBreakSeconds", 30);

    var policy =
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(static response => !response.IsSuccessStatusCode)
            .AdvancedCircuitBreakerAsync(
                failureThreshold: threshold,
                samplingDuration: TimeSpan.FromSeconds(30),
                minimumThroughput: minimumThroughput,
                durationOfBreak: TimeSpan.FromSeconds(breakDuration));

    registry.Add("http:CircuitBreaker", policy);
  }

  private static void RegisterTimeoutPolicy(
      PolicyRegistry registry,
      IConfigurationSection section)
  {
    var timeout = section.GetSection("TimeoutPolicy");

    if (!timeout.Exists() || !timeout.GetValue<bool>("Enabled"))
      return;

    var timeoutSeconds = timeout.GetValue("TimeoutSeconds", 5);

    var policy =
        Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeoutStrategy.Optimistic);

    registry.Add("http:TimeoutPolicy", policy);
  }

  private static void RegisterBulkheadPolicy(
      PolicyRegistry registry,
      IConfigurationSection section)
  {
    var bulkhead = section.GetSection("BulkheadPolicy");

    if (!bulkhead.Exists() || !bulkhead.GetValue<bool>("Enabled"))
      return;

    var maxParallelization = bulkhead.GetValue("MaxParallelization", 10);
    var maxQueueSize = bulkhead.GetValue("MaxQueueSize", 20);

    var policy =
        Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization,
            maxQueueSize);

    registry.Add("http:BulkheadPolicy", policy);
  }
}