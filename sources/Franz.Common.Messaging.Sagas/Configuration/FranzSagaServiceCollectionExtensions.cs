#nullable enable

using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Logging;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.EntityFramework;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Franz.Common.Messaging.Sagas.Configuration;

/// <summary>
/// DI extension entrypoint for configuring Franz sagas.
/// </summary>
public static class FranzSagaServiceCollectionExtensions
{
  public static FranzSagaBuilder AddFranzSagas(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    var section = configuration.GetSection("Franz:Sagas");
    var options = section.Get<FranzSagaOptions>() ?? new FranzSagaOptions();

    services.Configure<FranzSagaOptions>(section);

    // Register serializer (always required)
    services.AddSingleton<ISagaStateSerializer, JsonSagaStateSerializer>();

    // Register persistence provider
    RegisterPersistence(services, options);

    // Register pipeline
    services.AddSingleton<SagaExecutionPipeline>();

    // Register orchestrator
    services.AddSingleton<SagaOrchestrator>();

    // Register audit sink only if auditing enabled
    if (options.EnableAuditing)
    {
      services.TryAddEnumerable(ServiceDescriptor.Singleton<ISagaAuditSink, DefaultSagaAuditSink>());
    }

    return new FranzSagaBuilder(services);
  }

  private static void RegisterPersistence(IServiceCollection services, FranzSagaOptions options)
  {
    switch (options.Persistence.ToLowerInvariant())
    {
      case "memory":
        services.AddSingleton<InMemorySagaStateStore>();
        services.AddSingleton<ISagaRepository, InMemorySagaRepository>();
        break;

      case "entityframework":
        services.AddDbContext<SagaDbContext>(ConfigureDbContext);
        services.AddScoped<ISagaRepository, EFSagaRepository>();
        break;

      case "redis":
        // Redis currently not implemented (stub)
        services.AddSingleton<ISagaRepository, Persistence.Redis.RedisSagaRepository>();
        break;

      case "kafka":
        // Kafka persistence stub (future)
        services.AddSingleton<ISagaRepository, Persistence.Kafka.KafkaSagaRepository>();
        break;

      default:
        throw new InvalidOperationException(
            $"Unknown saga persistence provider '{options.Persistence}'.");
    }
  }

  private static void ConfigureDbContext(DbContextOptionsBuilder builder)
  {
    // The application will configure the actual provider 
    // (UseSqlServer, UseNpgsql, UseSqlite, etc.)
    //
    // Example:
    // services.AddDbContext<SagaDbContext>(opts => opts.UseSqlServer(...));
  }

  /// <summary>
  /// Completes registration for all sagas declared through the builder.
  /// Call after registering all sagas.
  /// </summary>
  public static IServiceCollection BuildFranzSagas(
      this IServiceCollection services,
      IServiceProvider provider)
  {
    var options = provider.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<FranzSagaOptions>>().Value;

    var builder = provider.GetRequiredService<FranzSagaBuilder>();
    builder.Build(options.EnableValidation);

    return services;
  }
}
