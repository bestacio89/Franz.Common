#nullable enable

using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Franz.Common.Messaging.Sagas.Configuration;

public static class FranzSagaServiceCollectionExtensions
{
  /// <summary>
  /// Registers Franz Saga infrastructure and returns the builder
  /// so callers can register saga types.
  /// </summary>
  public static FranzSagaBuilder AddFranzSagas(
      this IServiceCollection services,
      Action<FranzSagaOptions>? configure = null)
  {
    var options = new FranzSagaOptions();
    configure?.Invoke(options);

    services.AddSingleton(options);

    // Core infrastructure
    services.AddSingleton<SagaRouter>();
    services.AddTransient<SagaExecutionPipeline>();
    services.AddTransient<SagaOrchestrator>();

    // Default audit sink
    services.TryAddSingleton<ISagaAuditSink, DefaultSagaAuditSink>();

    // Create + register the builder
    var builder = new FranzSagaBuilder(services);
    services.AddSingleton(builder);

    return builder;
  }

  /// <summary>
  /// Finalizes saga registrations.
  /// </summary>
  public static void BuildFranzSagas(this IServiceProvider provider)
  {
    var builder = provider.GetRequiredService<FranzSagaBuilder>();
    var options = provider.GetRequiredService<FranzSagaOptions>();

    builder.Build(provider, options.ValidateMappings);
  }
}
