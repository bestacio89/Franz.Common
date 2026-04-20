using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.AzureCosmosDB.Messaging;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.AzureCosmosDB.Repositories;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Business.Repositories;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cosmos.Storage.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Franz.Common.AzureCosmosDB.Extensions;

public static class CosmosServiceCollectionExtensions
{
  private const string DefaultSection = "Cosmos";

  public static IServiceCollection AddFranzCosmosDbContext<TContext>(
      this IServiceCollection services,
      IConfiguration configuration,
      bool validateOnStart = true)
      where TContext : CosmosDbContextBase
  {
    services.ConfigureCosmosOptions(configuration, validateOnStart);

    services.AddDbContext<TContext>((sp, options) =>
    {
      var cosmos = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;

      if (!cosmos.Enabled)
        return;

      options.UseCosmos(
          cosmos.AccountEndpoint,
          cosmos.AccountKey,
          cosmos.DatabaseName,
          cosmosBuilder =>
              ConfigureCosmosProvider(cosmosBuilder, cosmos)

      );
    });

    return services;
  }

  public static IServiceCollection AddCosmosInfrastructure<TDbContext>(
      this IServiceCollection services,
      IConfiguration configuration)
      where TDbContext : CosmosDbContextBase
  {
    // 1. Core EF Options & Context
    services.AddFranzCosmosDbContext<TDbContext>(configuration);

    // 2. Cosmos-Native Discovery Logic
    // Rerouted from AddEntityRepositories to avoid the Arity-2 relational crash
    services.AddCosmosEntityRepositories<TDbContext>();

    // 3. Domain Infrastructure
    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();
    services.AddTransient(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));

    return services;
  }

  public static IServiceCollection AddFranzCosmosMessaging(
      this IServiceCollection services,
      IConfiguration configuration,
    bool validateonstart = true)
  {
    services.ConfigureCosmosOptions(configuration, validateonstart);

    services.AddDbContext<CosmosMessagingDbContext>((sp, options) =>
    {
      var cosmos = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
      if (!cosmos.Enabled) return;

      options.UseCosmos(
          cosmos.AccountEndpoint,
          cosmos.AccountKey,
          cosmos.DatabaseName,
          cosmosBuilder =>
          {
            // EF Core 10 handles container mapping via ModelBuilder.ToContainer()
            // in the DbContext, so we avoid setting a default container here
            // to prevent mapping collisions.
            ConfigureCosmosProvider(cosmosBuilder, cosmos);
          }
      );
    });

    services.AddScoped<IMessageStore, CosmosEfMessageStore>();
    services.AddScoped<IInboxStore, CosmosEfInboxStore>();

    return services;
  }

  private static void ConfigureCosmosOptions(
      this IServiceCollection services,
      IConfiguration configuration,
      bool validateOnStart)
  {
    var builder = services
        .AddOptions<CosmosOptions>()
        .Bind(configuration.GetSection(DefaultSection))
        .ValidateDataAnnotations();

    if (validateOnStart)
    {
      builder.Validate(o =>
      {
        if (string.IsNullOrWhiteSpace(o.AccountEndpoint))
          throw new ValidationException("Missing AccountEndpoint");

        if (string.IsNullOrWhiteSpace(o.AccountKey))
          throw new ValidationException("Missing AccountKey");

        if (string.IsNullOrWhiteSpace(o.DatabaseName))
          throw new ValidationException("Missing DatabaseName");

        return true;
      });
    }
  }

  private static void ConfigureCosmosProvider(
      Microsoft.EntityFrameworkCore.Infrastructure.CosmosDbContextOptionsBuilder builder,
      CosmosOptions settings)
  {
    builder.HttpClientFactory(() =>
    {
      var handler = new HttpClientHandler();

      // 🔥 TEST SAFETY: allow emulator / self-signed certs
      if (settings.Enabled && settings.ApplicationName?.Contains("Tests") == true)
      {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
      }

      var client = new HttpClient(handler);

      if (!string.IsNullOrWhiteSpace(settings.ApplicationName))
      {
        client.DefaultRequestHeaders.Add(
            "User-Agent",
            settings.ApplicationName);
      }

      return client;
    });
    builder.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
    builder.ExecutionStrategy(d =>
        new CosmosExecutionStrategy(d));

    builder.LimitToEndpoint();
  }

  public static IServiceCollection AddCosmosEntityRepositories<TDbContext>(
      this IServiceCollection services)
      where TDbContext : CosmosDbContextBase
  {
    // 1. Register the context as the base DbContext for generic use
    services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

    // 2. Reflectively discover entities
    var entityTypes = GetEfEntityTypes(typeof(TDbContext));

    foreach (var entityType in entityTypes)
    {
      // Extract the TId from the Entity<TId> base
      var idType = entityType.BaseType?.GetGenericArguments().FirstOrDefault() ?? typeof(Guid);

      var serviceType = typeof(IEntityRepository<,>).MakeGenericType(entityType, idType);

      // Cosmos-specific implementation binding (Arity-3)
      var implementationType = typeof(CosmosEntityRepository<,,>)
          .MakeGenericType(typeof(TDbContext), entityType, idType);

      services.AddNoDuplicateScoped(serviceType, implementationType);
    }

    return services;
  }

  private static IEnumerable<Type> GetEfEntityTypes(Type dbContextType)
  {
    return dbContextType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        .Select(p => p.PropertyType.GetGenericArguments().Single())
        .Where(t => typeof(IEntity).IsAssignableFrom(t))
        .ToList();
  }
}