#nullable enable
using Franz.Common.Http.Refit.Contracts;
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
    /// <summary>
    /// Registers a typed Refit client with Franz conventions:
    /// - Adds standard headers via <see cref="FranzRefitHeadersHandler"/>
    /// - Adds authentication via <see cref="FranzRefitAuthHandler"/> (auto-disabled if no provider or options)
    /// - Optionally attaches a Polly policy from the registry
    /// - Supports OpenTelemetry-friendly RefitSettings
    /// </summary>
    public static IServiceCollection AddFranzRefit<TClient>(
        this IServiceCollection services,
        string name,
        string baseUrl,
        string? policyName = null,
        Action<RefitSettings>? configureRefitSettings = null,
        Action<RefitClientOptions>? configureOptions = null)
        where TClient : class
    {
      // Register handlers as singletons (stateless, safe)
      services.TryAddSingleton<FranzRefitHeadersHandler>();
      services.TryAddSingleton<FranzRefitAuthHandler>();

      // Register options if provided
      if (configureOptions != null)
        services.Configure(configureOptions);

      // Configure Refit client
      IHttpClientBuilder refitBuilder;
      if (configureRefitSettings is not null)
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

      // Apply base address
      refitBuilder.ConfigureHttpClient((_, client) => client.BaseAddress = new Uri(baseUrl));

      // Add headers handler
      refitBuilder.AddHttpMessageHandler<FranzRefitHeadersHandler>();

      // Add authentication handler
      refitBuilder.AddHttpMessageHandler(sp =>
      {
        var tokenProvider = sp.GetService<ITokenProvider>(); // optional
        var options = sp.GetService<RefitClientOptions>();   // optional
        var logger = sp.GetRequiredService<ILogger<FranzRefitAuthHandler>>();

        // 🧠 If no token provider, return a no-op handler that disables itself
        return new FranzRefitAuthHandler(
          tokenProvider ?? new NoOpTokenProvider(),
          options,
          logger);
      });

      // Attach named Polly policy if specified
      if (!string.IsNullOrWhiteSpace(policyName))
      {
        refitBuilder.AddPolicyHandlerFromRegistry(policyName);
      }

      return services;
    }

    /// <summary>
    /// Minimal no-op token provider that always returns null.
    /// Used to gracefully disable auth handling when no provider is registered.
    /// </summary>
    private sealed class NoOpTokenProvider : ITokenProvider
    {
      public Task<string?> GetTokenAsync(CancellationToken ct = default)
          => Task.FromResult<string?>(null);
    }
  }
}
