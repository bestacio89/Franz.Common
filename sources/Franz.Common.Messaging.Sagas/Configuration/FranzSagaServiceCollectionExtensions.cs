#nullable enable

using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Franz.Common.Messaging.Sagas.Configuration;

public static class FranzSagaServiceCollectionExtensions
{
  public static IServiceCollection AddFranzSagas(
      this IServiceCollection services,
      Action<FranzSagaOptions>? configure = null)
  {
    var options = new FranzSagaOptions();
    configure?.Invoke(options);

    services.AddSingleton(options);

    // Register router + orchestrator dependencies
    services.AddSingleton<SagaRouter>();
    services.AddTransient<SagaExecutionPipeline>();
    services.AddTransient<SagaOrchestrator>();

    // Default audit sink if none registered
    services.TryAddSingleton<ISagaAuditSink, DefaultSagaAuditSink>();

    // The builder gets injected so we can finalize registration later
    var builder = new FranzSagaBuilder(services);

    return services;
  }

  /// <summary>
  /// Must be called at the END of DI building (inside app startup).
  /// Ensures all sagas are validated and registered.
  /// </summary>
  public static void BuildFranzSagas(this IServiceProvider provider)
  {
    var builder = provider.GetRequiredService<FranzSagaBuilder>();
    var options = provider.GetRequiredService<FranzSagaOptions>();

    // FIX: pass BOTH arguments as required by the builder signature
    builder.Build(provider, options.ValidateMappings);
  }
}
