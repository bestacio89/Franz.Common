#nullable enable

using Franz.Common.EntityFramework.Extensions;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Storage;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Messaging.Sagas.Handlers;
using Franz.Common.Messaging.Sagas.Messages;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.Cosmos;
using Franz.Common.Messaging.Sagas.Persistence.EntityFramework;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Mongo;
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

    if (options.MaxRetryAttempts < 1)
      throw new ArgumentOutOfRangeException(
          nameof(options.MaxRetryAttempts),
          options.MaxRetryAttempts,
          "Saga MaxRetryAttempts must be greater than zero.");

    if (options.DefaultRetryDelay <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(
          nameof(options.DefaultRetryDelay),
          options.DefaultRetryDelay,
          "Saga DefaultRetryDelay must be greater than zero.");

    if (options.AuditRetentionPeriod is { } retentionPeriod && retentionPeriod <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(
          nameof(options.AuditRetentionPeriod),
          retentionPeriod,
          "Saga audit retention period must be greater than zero.");

    if (options.AuditRetentionCleanupInterval <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(
          nameof(options.AuditRetentionCleanupInterval),
          options.AuditRetentionCleanupInterval,
          "Saga audit retention cleanup interval must be greater than zero.");

    if (options.AuditRetentionBatchSize <= 0)
      throw new ArgumentOutOfRangeException(
          nameof(options.AuditRetentionBatchSize),
          options.AuditRetentionBatchSize,
          "Saga audit retention batch size must be greater than zero.");

    if (options.AuditRetentionPeriod.HasValue &&
        options.Persistence.Equals("Memory", StringComparison.OrdinalIgnoreCase))
      throw new NotSupportedException(
          "Saga audit retention requires durable EF Core, MongoDB, or Cosmos persistence.");

    if (!new[] { "Memory", "EntityFramework", "Mongo", "Cosmos" }
        .Contains(options.Persistence, StringComparer.OrdinalIgnoreCase))
      throw new NotSupportedException(
          $"Saga persistence provider '{options.Persistence}' is not supported.");

    services.AddSingleton(options);

    services.TryAddSingleton<SagaRouter>();
    services.TryAddSingleton<SagaExecutionPipeline>();
    services.TryAddTransient<SagaOrchestrator>();
    services.TryAddScoped<ISagaStateSerializer, JsonSagaStateSerializer>();

    services.TryAddEnumerable(
        ServiceDescriptor.Singleton<ISagaAuditSink, DefaultSagaAuditSink>());

    RegisterPersistenceAdapter(services, options.Persistence);

    if (options.AuditRetentionPeriod.HasValue)
      services.TryAddEnumerable(
          ServiceDescriptor.Singleton<IHostedService, SagaAuditRetentionHostedService>());

    services.TryAddEnumerable(ServiceDescriptor.Scoped(
        typeof(INotificationHandler<SagaRetryRequested>),
        typeof(SagaIntegrationEventNotificationHandler<SagaRetryRequested>)));

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

  private static void RegisterPersistenceAdapter(
      IServiceCollection services,
      string persistence)
  {
    if (persistence.Equals("Memory", StringComparison.OrdinalIgnoreCase))
      services.TryAddSingleton<InMemorySagaStateStore>();

    switch (persistence.ToUpperInvariant())
    {
      case "MEMORY":
        RegisterRepository<InMemorySagaRepository>(services);
        break;

      case "ENTITYFRAMEWORK":
        RegisterRepository<EFSagaRepository>(services);
        services.AddUnitOfWork<SagaDbContext>();
        services.AddScoped<IInboxStore, EfSagaInboxStore>();
        services.AddScoped<IMessageStore, EfSagaMessageStore>();
        RegisterAuditStore<EfSagaAuditStore>(services);
        break;

      case "MONGO":
        RegisterRepository<MongoSagaRepository>(services);
        services.TryAddScoped<MongoSagaUnitOfWork>();
        services.AddScoped<Franz.Common.Mediator.Pipelines.Core.IUnitOfWork>(
            provider => provider.GetRequiredService<MongoSagaUnitOfWork>());
        services.AddScoped<IInboxStore, MongoSagaInboxStore>();
        services.AddScoped<IMessageStore, MongoSagaMessageStore>();
        RegisterAuditStore<MongoSagaAuditStore>(services);
        break;

      case "COSMOS":
        RegisterRepository<CosmosSagaRepository>(services);
        services.TryAddScoped<CosmosSagaUnitOfWork>();
        services.AddScoped<Franz.Common.Mediator.Pipelines.Core.IUnitOfWork>(
            provider => provider.GetRequiredService<CosmosSagaUnitOfWork>());
        services.AddScoped<IInboxStore, CosmosSagaInboxStore>();
        services.AddScoped<IMessageStore, CosmosSagaMessageStore>();
        RegisterAuditStore<CosmosSagaAuditStore>(services);
        break;

      default:
        throw new NotSupportedException(
            $"Saga persistence provider '{persistence}' is not supported.");
    }
  }

  private static void RegisterAuditStore<TAuditStore>(IServiceCollection services)
      where TAuditStore : class, ISagaAuditStore
  {
    services.TryAddScoped<TAuditStore>();
    services.TryAddScoped<ISagaAuditStore>(
        provider => provider.GetRequiredService<TAuditStore>());
    services.AddScoped<ISagaAuditSink>(
        provider => provider.GetRequiredService<ISagaAuditStore>());
  }

  private static void RegisterRepository<TRepository>(IServiceCollection services)
      where TRepository : class, ISagaRepository
  {
    services.TryAddScoped<TRepository>();
    services.TryAddScoped<ISagaRepository>(
        provider => provider.GetRequiredService<TRepository>());
  }
}
