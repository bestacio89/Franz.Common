#nullable enable
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
    ArgumentNullException.ThrowIfNull(configuration);

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
          cosmosBuilder => ConfigureCosmosProvider(cosmosBuilder, cosmos)
      );
    });

    return services;
  }

  public static IServiceCollection AddCosmosInfrastructure<TDbContext>(
      this IServiceCollection services,
      IConfiguration configuration)
      where TDbContext : CosmosDbContextBase
  {
    ArgumentNullException.ThrowIfNull(configuration);

    services.AddFranzCosmosDbContext<TDbContext>(configuration);

    services.AddCosmosEntityRepositories<TDbContext>();

    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();
    services.AddTransient(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));

    return services;
  }

  public static IServiceCollection AddFranzCosmosMessaging(
      this IServiceCollection services,
      IConfiguration configuration,
      bool validateOnStart = true)
  {
    ArgumentNullException.ThrowIfNull(configuration);

    services.ConfigureCosmosOptions(configuration, validateOnStart);

    services.AddDbContext<CosmosMessagingDbContext>((sp, options) =>
    {
      var cosmos = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
      if (!cosmos.Enabled) return;

      options.UseCosmos(
          cosmos.AccountEndpoint,
          cosmos.AccountKey,
          cosmos.DatabaseName,
          cosmosBuilder => ConfigureCosmosProvider(cosmosBuilder, cosmos)
      );
    });

    services.AddScoped<IMessageStore, CosmosEfMessageStore>();
    services.AddScoped<IInboxStore, CosmosEfInboxStore>();

    return services;
  }

  // =====================================================
  // Options
  // =====================================================

  private static void ConfigureCosmosOptions(
      this IServiceCollection services,
      IConfiguration configuration,
      bool validateOnStart)
  {
    ArgumentNullException.ThrowIfNull(configuration);

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

      builder.ValidateOnStart();
    }
  }

  // =====================================================
  // Cosmos provider configuration (FIXED EF warning)
  // =====================================================

  private static void ConfigureCosmosProvider(
      Microsoft.EntityFrameworkCore.Infrastructure.CosmosDbContextOptionsBuilder builder,
      CosmosOptions settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    builder.HttpClientFactory(() =>
    {
      var handler = new HttpClientHandler();

      if (settings.Enabled &&
          settings.ApplicationName?.Contains("Tests", StringComparison.OrdinalIgnoreCase) == true)
      {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
      }

      var client = new HttpClient(handler);

      if (!string.IsNullOrWhiteSpace(settings.ApplicationName))
      {
        client.DefaultRequestHeaders.Add("User-Agent", settings.ApplicationName);
      }

      return client;
    });

    builder.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);

    // ❌ FIX: removed internal EF API usage
    // builder.ExecutionStrategy(d => new CosmosExecutionStrategy(d));

    builder.LimitToEndpoint();
  }

  // =====================================================
  // Repository discovery
  // =====================================================

  public static IServiceCollection AddCosmosEntityRepositories<TDbContext>(
      this IServiceCollection services)
      where TDbContext : CosmosDbContextBase
  {
    services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

    var entityTypes = GetEfEntityTypes(typeof(TDbContext));

    foreach (var entityType in entityTypes)
    {
      var idType =
          entityType.BaseType?.GetGenericArguments().FirstOrDefault()
          ?? typeof(Guid);

      var serviceType =
          typeof(IEntityRepository<,>).MakeGenericType(entityType, idType);

      var implementationType =
          typeof(CosmosEntityRepository<,,>).MakeGenericType(typeof(TDbContext), entityType, idType);

      services.AddNoDuplicateScoped(serviceType, implementationType);
    }

    return services;
  }

  private static IEnumerable<Type> GetEfEntityTypes(Type dbContextType)
  {
    return dbContextType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p =>
            p.PropertyType.IsGenericType &&
            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        .Select(p => p.PropertyType.GetGenericArguments().Single())
        .Where(t => typeof(IEntity).IsAssignableFrom(t))
        .ToList();
  }
}