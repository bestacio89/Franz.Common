using Franz.Common.Mediator.Results;
using Polly;
using Polly.Bulkhead;
using Polly.Timeout;
using System;
using System.Net.Http;

namespace Franz.Common.Mediator.Polly.Options
{
  public static class PollyPolicyRegistryOptionsExtensions
  {
    // ================================================
    // DOMAIN / MEDIATOR POLICIES
    // ================================================
    public static void AddMediatorRetry<TResponse>(this PollyPolicyRegistryOptions options, string name, int retryCount, int intervalMs)
    {
      var policy = Policy<Result<TResponse>>
        .Handle<Exception>()
        .OrResult(r => !r.IsSuccess) // assuming your Result<T> has IsSuccess flag
        .WaitAndRetryAsync(retryCount, _ => TimeSpan.FromMilliseconds(intervalMs));

      options.MediatorPolicies[name] = policy;
    }

    public static void AddMediatorCircuitBreaker<TResponse>(this PollyPolicyRegistryOptions options, string name, double failureThreshold, int minimumThroughput, int durationSeconds)
    {
      var policy = Policy<Result<TResponse>>
        .Handle<Exception>()
        .OrResult(r => !r.IsSuccess)
        .AdvancedCircuitBreakerAsync(
          failureThreshold,
          TimeSpan.FromSeconds(30),
          minimumThroughput,
          TimeSpan.FromSeconds(durationSeconds));

      options.MediatorPolicies[name] = policy;
    }

    public static void AddMediatorTimeout<TResponse>(this PollyPolicyRegistryOptions options, string name, int timeoutSeconds)
    {
      var policy = Policy.TimeoutAsync<Result<TResponse>>(TimeSpan.FromSeconds(timeoutSeconds), TimeoutStrategy.Optimistic);
      options.MediatorPolicies[name] = policy;
    }

    public static void AddMediatorBulkhead<TResponse>(this PollyPolicyRegistryOptions options, string name, int maxParallelization, int maxQueueSize)
    {
      var policy = Policy.BulkheadAsync<Result<TResponse>>(maxParallelization, maxQueueSize);
      options.MediatorPolicies[name] = policy;
    }

    // ================================================
    // HTTP POLICIES
    // ================================================
    public static void AddHttpRetry(this PollyPolicyRegistryOptions options, string name, int retryCount, int intervalMs)
    {
      var policy = Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(retryCount, _ => TimeSpan.FromMilliseconds(intervalMs));

      options.HttpPolicies[name] = policy;
    }

    public static void AddHttpCircuitBreaker(this PollyPolicyRegistryOptions options, string name, double failureThreshold, int minimumThroughput, int durationSeconds)
    {
      var policy = Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => !r.IsSuccessStatusCode)
        .AdvancedCircuitBreakerAsync(
          failureThreshold,
          TimeSpan.FromSeconds(30),
          minimumThroughput,
          TimeSpan.FromSeconds(durationSeconds));

      options.HttpPolicies[name] = policy;
    }

    public static void AddHttpTimeout(this PollyPolicyRegistryOptions options, string name, int timeoutSeconds)
    {
      var policy = Policy.TimeoutAsync<HttpResponseMessage>(
        TimeSpan.FromSeconds(timeoutSeconds),
        TimeoutStrategy.Optimistic);

      options.HttpPolicies[name] = policy;
    }

    public static void AddHttpBulkhead(this PollyPolicyRegistryOptions options, string name, int maxParallelization, int maxQueueSize)
    {
      var policy = Policy.BulkheadAsync<HttpResponseMessage>(
        maxParallelization,
        maxQueueSize);

      options.HttpPolicies[name] = policy;
    }
  }
}
