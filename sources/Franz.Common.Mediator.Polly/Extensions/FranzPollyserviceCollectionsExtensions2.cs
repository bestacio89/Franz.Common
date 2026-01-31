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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Franz.Common.Mediator.Polly
{
  /// <summary>
  /// Unified Franz Polly integration.
  /// Provides Mediator + HTTP resilience policies with profile-based composition.
  /// </summary>
  public static class FranzPollyServiceCollectionExtensions2
  {
    public static IServiceCollection AddFranzResiliencev2(this IServiceCollection services, IConfiguration configuration)
    {
      var resilience = configuration.GetSection("Resilience");
      if (!resilience.Exists())
        return services;

      var registry = new PolicyRegistry();

      // Step 1: register base policies
      RegisterMediatorPolicies(registry, resilience);
      RegisterHttpPolicies(registry, resilience);

      // Step 2: compose profiles
      var mediatorProfiles = resilience.GetSection("MediatorProfiles");
      RegisterProfiles(registry, mediatorProfiles, "mediator", isHttp: false);

      var httpProfiles = resilience.GetSection("HttpProfiles");
      RegisterProfiles(registry, httpProfiles, "http", isHttp: true);

      // Step 3: register policy registry and single mediator pipeline
      services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);
      services.AddTransient(typeof(IPipeline<,>), typeof(PollyResiliencePipeline<,>));

      Console.WriteLine("🧭 Franz.PolicyRegistry initialized with:");
      foreach (var kvp in registry)
        Console.WriteLine($"   {kvp.Key}");

      return services;
    }

    // ==========================================================
    // BASE POLICY REGISTRATION
    // ==========================================================
    private static void RegisterMediatorPolicies(PolicyRegistry registry, IConfigurationSection section)
    {
      if (section.GetSection("RetryPolicy").GetValue<bool>("Enabled"))
      {
        registry.Add("mediator:Retry",
            Policy.Handle<Exception>()
                  .WaitAndRetryAsync(
                      section.GetValue<int>("RetryCount", 3),
                      _ => TimeSpan.FromMilliseconds(section.GetValue<int>("RetryIntervalMilliseconds", 200))));
      }

      if (section.GetSection("TimeoutPolicy").GetValue<bool>("Enabled"))
      {
        registry.Add("mediator:Timeout",
            Policy.TimeoutAsync(
                TimeSpan.FromSeconds(section.GetValue<int>("TimeoutSeconds", 5)),
                TimeoutStrategy.Optimistic));
      }

      if (section.GetSection("CircuitBreaker").GetValue<bool>("Enabled"))
      {
        registry.Add("mediator:CircuitBreaker",
            Policy.Handle<Exception>()
                  .AdvancedCircuitBreakerAsync(
                      failureThreshold: section.GetValue<double>("FailureThreshold", 0.5),
                      samplingDuration: TimeSpan.FromSeconds(30),
                      minimumThroughput: section.GetValue<int>("MinimumThroughput", 10),
                      durationOfBreak: TimeSpan.FromSeconds(section.GetValue<int>("DurationOfBreakSeconds", 30))));
      }

      if (section.GetSection("BulkheadPolicy").GetValue<bool>("Enabled"))
      {
        registry.Add("mediator:Bulkhead",
            Policy.BulkheadAsync(
                section.GetValue<int>("MaxParallelization", 10),
                section.GetValue<int>("MaxQueueSize", 20)));
      }
    }

    private static void RegisterHttpPolicies(PolicyRegistry registry, IConfigurationSection section)
    {
      if (section.GetSection("RetryPolicy").GetValue<bool>("Enabled"))
      {
        registry.Add("http:Retry",
            Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    section.GetValue<int>("RetryCount", 3),
                    _ => TimeSpan.FromMilliseconds(section.GetValue<int>("RetryIntervalMilliseconds", 200))));
      }

      if (section.GetSection("TimeoutPolicy").GetValue<bool>("Enabled"))
      {
        registry.Add("http:Timeout",
            Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(section.GetValue<int>("TimeoutSeconds", 5)),
                TimeoutStrategy.Optimistic));
      }

      if (section.GetSection("CircuitBreaker").GetValue<bool>("Enabled"))
      {
        registry.Add("http:CircuitBreaker",
            Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: section.GetValue<double>("FailureThreshold", 0.5),
                    samplingDuration: TimeSpan.FromSeconds(30),
                    minimumThroughput: section.GetValue<int>("MinimumThroughput", 10),
                    durationOfBreak: TimeSpan.FromSeconds(section.GetValue<int>("DurationOfBreakSeconds", 30))));
      }

      if (section.GetSection("BulkheadPolicy").GetValue<bool>("Enabled"))
      {
        registry.Add("http:Bulkhead",
            Policy.BulkheadAsync<HttpResponseMessage>(
                section.GetValue<int>("MaxParallelization", 10),
                section.GetValue<int>("MaxQueueSize", 20)));
      }
    }

    // ==========================================================
    // PROFILE COMPOSITION
    // ==========================================================
    private static void RegisterProfiles(PolicyRegistry registry, IConfigurationSection profiles, string prefix, bool isHttp)
    {
      foreach (var profile in profiles.GetChildren())
      {
        var policies = profile.GetSection("Policies").Get<string[]>() ?? Array.Empty<string>();

        if (isHttp)
        {
          var compositeHttp = BuildCompositePolicyHttp(registry, policies);
          registry.Add($"{prefix}:{profile.Key}", compositeHttp);
        }
        else
        {
          var composite = BuildCompositePolicy(registry, policies);
          registry.Add($"{prefix}:{profile.Key}", composite);
        }
      }
    }

    private static IAsyncPolicy BuildCompositePolicy(PolicyRegistry registry, IEnumerable<string> policyNames)
    {
      var policies = policyNames
          .Select(name =>
              registry.TryGet<IAsyncPolicy>(name, out var policy)
                  ? policy
                  : throw new InvalidOperationException($"Resilience policy '{name}' is not registered"))
          .Reverse()
          .ToList();

      IAsyncPolicy composite = Policy.NoOpAsync();

      foreach (var policy in policies)
        composite = policy.WrapAsync(composite);

      return composite;
    }

    private static IAsyncPolicy<HttpResponseMessage> BuildCompositePolicyHttp(PolicyRegistry registry, IEnumerable<string> policyNames)
    {
      var policies = policyNames
          .Select(name =>
              registry.TryGet<IAsyncPolicy<HttpResponseMessage>>(name, out var policy)
                  ? policy
                  : throw new InvalidOperationException($"Resilience policy '{name}' is not registered"))
          .Reverse()
          .ToList();

      IAsyncPolicy<HttpResponseMessage> composite = Policy.NoOpAsync<HttpResponseMessage>();

      foreach (var policy in policies)
        composite = policy.WrapAsync(composite);

      return composite;
    }
  }
}
