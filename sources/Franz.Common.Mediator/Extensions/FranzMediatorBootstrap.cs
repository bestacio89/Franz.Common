using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Franz.Common.Mediator.Bootstrap;

public static class FranzMediatorBootstrap
{
  /// <summary>
  /// Standard production-ready mediator setup:
  /// - Core mediator (dispatcher + handlers)
  /// - Logging pipeline
  /// - Validation pipeline
  /// - Serilog logging pipeline
  /// - Serilog audit pipeline
  /// - Event validation pipeline
  /// - Transaction pipeline
  /// </summary>
  public static IServiceCollection AddFranzMediatorStandard(
      this IServiceCollection services,
      Assembly[] assemblies,
      Action<FranzMediatorOptions>? configure = null)
  {
    // 1. Core mediator (dispatcher + handlers)
    services.AddFranzMediator(assemblies, configure);

    // 2. Cross-cutting pipelines (ordered intentionally)

    // Logging first → observe everything
    services.AddFranzLoggingPipeline();

    // Validation early → fail fast before execution
    services.AddFranzValidationPipeline();

    // Event-level validation & auditing pipeline
    services.AddFranzEventValidationPipeline();

    // Serilog enrichment (logging + audit correlation context)
    services.AddFranzSerilogLoggingPipeline();
    services.AddFranzSerilogAuditPipeline();

    // Transaction boundary support
    services.AddFranzTransactionPipeline();

    return services;
  }
}