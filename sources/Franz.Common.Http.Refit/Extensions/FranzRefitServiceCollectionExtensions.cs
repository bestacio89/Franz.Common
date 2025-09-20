#nullable enable
using Franz.Common.Http.Refit.Handlers;
using Franz.Common.Http.Refit.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Http.Refit.Extensions
{
  public static class FranzRefitServiceCollectionExtensions
  {
    public static IServiceCollection AddFranzRefit<TClient>(
        this IServiceCollection services,
        string name,
        string baseUrl,
        string? policyName = null,
        Action<RefitSettings>? configureRefitSettings = null,
        Action<RefitClientOptions>? configureOptions = null)
        where TClient : class
    {
      // Register handlers (singleton is fine for handlers with DI)
      services.TryAddSingleton<FranzRefitHeadersHandler>();
      services.TryAddSingleton<FranzRefitAuthHandler>();

      if (configureOptions != null)
        services.Configure(configureOptions);

      // Choose correct AddRefitClient overload (factory returning RefitSettings to avoid ambiguity)
      IHttpClientBuilder refitBuilder;
      if (configureRefitSettings != null)
      {
        refitBuilder = services.AddRefitClient<TClient>(sp =>
        {
          var settings = new RefitSettings();
          configureRefitSettings(settings);
          return settings;
        });
      }
      else
      {
        refitBuilder = services.AddRefitClient<TClient>();
      }

      // Configure base address
      refitBuilder.ConfigureHttpClient((sp, client) => client.BaseAddress = new Uri(baseUrl));

      // Add header handler
      refitBuilder.AddHttpMessageHandler<FranzRefitHeadersHandler>();

      // Add auth handler: if an ITokenProvider is registered use it, otherwise use a NoOp implementation
      refitBuilder.AddHttpMessageHandler(sp =>
      {
        var tokenProvider = sp.GetService<ITokenProvider>();
        var logger = sp.GetRequiredService<ILogger<FranzRefitAuthHandler>>();

        return tokenProvider is null
          ? new FranzRefitAuthHandler(new NoOpTokenProvider(), logger) as DelegatingHandler
          : new FranzRefitAuthHandler(tokenProvider, logger) as DelegatingHandler;
      });

      // Optional: attach named Polly policy from registry (host app must register the policy)
      if (!string.IsNullOrWhiteSpace(policyName))
      {
        refitBuilder.AddPolicyHandlerFromRegistry(policyName);
      }

      return services;
    }

    // Minimal no-op token provider used if DI doesn't provide a real one
    private sealed class NoOpTokenProvider : ITokenProvider
    {
      public Task<string?> GetTokenAsync(CancellationToken ct = default) =>
          Task.FromResult<string?>(null);
    }
  }
}
