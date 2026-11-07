#nullable enable

using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Messaging.Sagas.Configuration;

public static class FranzSagaServiceCollectionExtensions
{
  public static FranzSagaBuilder AddFranzSagas(
      this IServiceCollection services,
      Action<FranzSagaOptions>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(services);

    var options = new FranzSagaOptions();
    configure?.Invoke(options);

    if (!new[] { "Memory", "EntityFramework", "Mongo", "Cosmos" }
        .Contains(options.Persistence, StringComparer.OrdinalIgnoreCase))
      throw new NotSupportedException(
          $"Saga persistence provider '{options.Persistence}' is not supported.");

    services.AddSingleton(options);

    services.TryAddSingleton<SagaRouter>();
    services.TryAddSingleton<SagaExecutionPipeline>();
    services.TryAddTransient<SagaOrchestrator>();
    services.TryAddScoped<ISagaStateSerializer, JsonSagaStateSerializer>();

    if (options.Persistence.Equals("Memory", StringComparison.OrdinalIgnoreCase))
    {
      services.TryAddSingleton<InMemorySagaStateStore>();
      services.TryAddScoped<InMemorySagaRepository>();
      services.TryAddScoped<ISagaRepository>(
          provider => provider.GetRequiredService<InMemorySagaRepository>());
    }

    services.TryAddSingleton<ISagaAuditSink, DefaultSagaAuditSink>();

    var builder = new FranzSagaBuilder(services);
    services.AddSingleton(builder);
    services.TryAddEnumerable(
        ServiceDescriptor.Singleton<IHostedService, SagaRegistrationHostedService>());

    return builder;
  }

  /// <summary>
  /// Explicit registration finalization for hosts that do not use IHostedService startup.
  /// The operation is idempotent because the router rejects duplicate saga registrations.
  /// </summary>
  public static void BuildFranzSagas(this IServiceProvider provider)
  {
    ArgumentNullException.ThrowIfNull(provider);

    var builder = provider.GetRequiredService<FranzSagaBuilder>();
    var options = provider.GetRequiredService<FranzSagaOptions>();
    builder.Build(provider, options.ValidateMappings);
  }
}
