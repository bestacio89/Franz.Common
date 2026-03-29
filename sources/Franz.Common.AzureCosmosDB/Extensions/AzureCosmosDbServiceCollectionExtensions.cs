using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.AzureCosmosDB.Messaging;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cosmos.Storage.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.AzureCosmosDB.Extensions;

public static class CosmosServiceCollectionExtensions
{
  private const string DefaultSection = "Cosmos";

  public static IServiceCollection AddFranzCosmosDbContext<TContext>(
      this IServiceCollection services,
      IConfiguration configuration)
      where TContext : CosmosDbContextBase
  {
    services.ConfigureCosmosOptions(configuration);

    services.AddDbContext<TContext>((sp, options) =>
    {
      var cosmos = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
      if (!cosmos.Enabled) return;

      // .NET 10 optimized connection
      options.UseCosmos(
          cosmos.AccountEndpoint,
          cosmos.AccountKey,
          cosmos.DatabaseName,
          cosmosBuilder => ConfigureCosmosProvider(cosmosBuilder, cosmos)
      );
    });

    return services;
  }

  public static IServiceCollection AddFranzCosmosMessaging(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    services.ConfigureCosmosOptions(configuration);

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

  private static void ConfigureCosmosOptions(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddOptions<CosmosOptions>()
        .Bind(configuration.GetSection(DefaultSection))
        .ValidateDataAnnotations()
        .ValidateOnStart();
  }

  private static void ConfigureCosmosProvider(
      Microsoft.EntityFrameworkCore.Infrastructure.CosmosDbContextOptionsBuilder builder,
      CosmosOptions settings)
  {
    if (!string.IsNullOrWhiteSpace(settings.ApplicationName))
    {
      builder.HttpClientFactory(() =>
      {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", settings.ApplicationName);
        return client;
      });
    }

    // EF Core 10 Best Practice: Always use the execution strategy for 
    // built-in Cosmos retries on transient failures.
    builder.ExecutionStrategy(d => new CosmosExecutionStrategy(d));

    // Use region-based routing if available
    builder.LimitToEndpoint();
  }
}