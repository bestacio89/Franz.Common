using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Business.Helpers;
using Franz.Common.Business.Properties;
using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Franz.Common.Business.Extensions;

public static class BusinessBootstrapExtensions
{
  /// <summary>
  /// Full Business + Mediator + Handlers + Domain wiring (production-safe).
  /// </summary>
  public static IServiceCollection AddBusiness(
      this IServiceCollection services,
      Assembly applicationAssembly,
      Action<FranzMediatorOptions>? configure = null)
  {
    if (applicationAssembly is null)
      throw new ArgumentNullException(nameof(applicationAssembly));

    // -----------------------------
    // DOMAIN LAYER
    // -----------------------------
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();
    services.AddTransient(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));

    // -----------------------------
    // MEDIATOR LAYER
    // -----------------------------
    services.AddFranzMediator(new[] { applicationAssembly }, configure);

    // -----------------------------
    // HANDLER DISCOVERY
    // -----------------------------
    HandlerCollector.CollectHandlers(services, applicationAssembly);

    return services;
  }

  /// <summary>
  /// Optional platform stack (logging + resilience pipelines).
  /// </summary>
  public static IServiceCollection AddBusinessPlatform(
      this IServiceCollection services)
  {
    services.AddFranzSerilogLoggingPipeline();
    services.AddFranzRetryPipeline();
    services.AddFranzCircuitBreakerPipeline();
    services.AddFranzTimeoutPipeline();
    services.AddFranzBulkheadPipeline();

    return services;
  }

  /// <summary>
  /// Safe fallback version (no exceptions if assembly missing).
  /// </summary>
  public static IServiceCollection TryAddBusiness(
      this IServiceCollection services,
      Assembly? applicationAssembly,
      Action<FranzMediatorOptions>? configure = null)
  {
    if (applicationAssembly is null)
      return services;

    return services.AddBusiness(applicationAssembly, configure);
  }
}