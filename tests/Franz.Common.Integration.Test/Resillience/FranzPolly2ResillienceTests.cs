using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Polly;
using Franz.Common.Mediator.Polly.Pipelines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Integration.Tests.Polly
{
  public class FranzPollyRegistrationTests2
  {
    // Dummy types for concrete pipeline registration
    public class TestRequest { }
    public class TestResponse { }

    private IServiceCollection BuildServiceCollectionWithConfig(out IConfiguration config)
    {
      var inMemorySettings = new Dictionary<string, string>
            {
                // Base policy settings
                {"Resilience:RetryPolicy:Enabled", "true"},
                {"Resilience:RetryPolicy:RetryCount", "2"},
                {"Resilience:RetryPolicy:RetryIntervalMilliseconds", "50"},
                {"Resilience:TimeoutPolicy:Enabled", "true"},
                {"Resilience:TimeoutPolicy:TimeoutSeconds", "1"},
                {"Resilience:CircuitBreaker:Enabled", "true"},
                {"Resilience:FailureThreshold", "0.5"},
                {"Resilience:MinimumThroughput", "2"},
                {"Resilience:DurationOfBreakSeconds", "2"},
                {"Resilience:BulkheadPolicy:Enabled", "true"},
                {"Resilience:MaxParallelization", "2"},
                {"Resilience:MaxQueueSize", "2"},

                // Profiles
                {"Resilience:MediatorProfiles:Default:Policies:0", "mediator:Retry"},
                {"Resilience:MediatorProfiles:Default:Policies:1", "mediator:Timeout"},
                {"Resilience:MediatorProfiles:Default:Policies:2", "mediator:CircuitBreaker"},
                {"Resilience:MediatorProfiles:Default:Policies:3", "mediator:Bulkhead"},

                {"Resilience:HttpProfiles:Default:Policies:0", "http:Retry"},
                {"Resilience:HttpProfiles:Default:Policies:1", "http:Timeout"},
                {"Resilience:HttpProfiles:Default:Policies:2", "http:CircuitBreaker"},
                {"Resilience:HttpProfiles:Default:Policies:3", "http:Bulkhead"}
            };

      config = new ConfigurationBuilder()
          .AddInMemoryCollection(inMemorySettings)
          .Build();

      var services = new ServiceCollection();
      services.AddLogging(); // Important for ILogger<TRequest> DI

      return services;
    }

    [Fact]
    public void TestPolicyRegistry_Registration()
    {
      var services = BuildServiceCollectionWithConfig(out var config);
      services.AddFranzResiliencev2(config);

      // Register concrete pipeline for test request/response
      services.AddTransient<IPipeline<TestRequest, TestResponse>, PollyResiliencePipeline<TestRequest, TestResponse>>();

      var provider = services.BuildServiceProvider();
      var registry = provider.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

      // Base mediator policies
      Assert.True(registry.ContainsKey("mediator:Retry"));
      Assert.True(registry.ContainsKey("mediator:Timeout"));
      Assert.True(registry.ContainsKey("mediator:CircuitBreaker"));
      Assert.True(registry.ContainsKey("mediator:Bulkhead"));

      // Base HTTP policies
      Assert.True(registry.ContainsKey("http:Retry"));
      Assert.True(registry.ContainsKey("http:Timeout"));
      Assert.True(registry.ContainsKey("http:CircuitBreaker"));
      Assert.True(registry.ContainsKey("http:Bulkhead"));

      // Composed profiles
      Assert.True(registry.ContainsKey("mediator:Default"));
      Assert.True(registry.ContainsKey("http:Default"));
    }

    [Fact]
    public async Task TestMediatorCompositePolicy_Execution()
    {
      var services = BuildServiceCollectionWithConfig(out var config);
      services.AddFranzResiliencev2(config);

      // Register concrete pipeline for test request/response
      services.AddTransient<IPipeline<TestRequest, TestResponse>, PollyResiliencePipeline<TestRequest, TestResponse>>();

      var provider = services.BuildServiceProvider();
      var registry = provider.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

      var policy = registry.Get<IAsyncPolicy>("mediator:Default");

      bool executed = false;
      await policy.ExecuteAsync(() =>
      {
        executed = true;
        return Task.CompletedTask;
      });

      Assert.True(executed, "Policy should execute the delegate successfully");
    }

    [Fact]
    public async Task TestHttpCompositePolicy_Execution()
    {
      var services = BuildServiceCollectionWithConfig(out var config);
      services.AddFranzResiliencev2(config);

      var provider = services.BuildServiceProvider();
      var registry = provider.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

      var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>("http:Default");

      bool executed = false;
      await policy.ExecuteAsync(() =>
      {
        executed = true;
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
      });

      Assert.True(executed, "HTTP composite policy should execute the delegate successfully");
    }

    [Fact]
    public void TestPipelineRegistration()
    {
      var services = BuildServiceCollectionWithConfig(out var config);
      services.AddFranzResiliencev2(config);

      // Register concrete pipeline for test request/response
      services.AddTransient<IPipeline<TestRequest, TestResponse>, PollyResiliencePipeline<TestRequest, TestResponse>>();

      var provider = services.BuildServiceProvider();

      var pipeline = provider.GetService<IPipeline<TestRequest, TestResponse>>();
      Assert.NotNull(pipeline); // Ensures the PollyResiliencePipeline is registered
    }
  }
}
