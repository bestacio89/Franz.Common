using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.AzureCosmosDB.Messaging;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.Messaging.Storage;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.AzureCosmosDB.Extensions;

public static class CosmosServiceCollectionExtensions
{
  /// <summary>
  /// Registers CosmosOptions and the EF Core Cosmos DbContext.
  /// </summary>
  public static IServiceCollection AddFranzCosmosDbContext<TContext>(
      this IServiceCollection services,
      IConfiguration configuration)
      where TContext : CosmosDbContextBase
  {
    // Bind CosmosOptions from "Cosmos" section
    services.Configure<CosmosOptions>(configuration.GetSection("Cosmos"));
    var cosmos = configuration.GetSection("Cosmos").Get<CosmosOptions>()
                 ?? throw new InvalidOperationException("Cosmos configuration missing.");

    if (!cosmos.Enabled)
      return services;

    // Register EF Core Cosmos DbContext
    services.AddDbContext<TContext>(options =>
    {
      options.UseCosmos(
          cosmos.AccountEndpoint,
          cosmos.AccountKey,
          cosmos.DatabaseName,
          cosmosOptions =>
          {
            if (!string.IsNullOrWhiteSpace(cosmos.ApplicationName))
            {
              cosmosOptions.HttpClientFactory(() =>
              {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", cosmos.ApplicationName);
                return client;
              });
            }

            if (cosmos.DatabaseThroughput.HasValue)
              cosmosOptions.LimitToEndpoint(cosmos.DatabaseThroughput.Value);
          }
      );
    });

    return services;
  }

  /// <summary>
  /// Registers the EF Core Cosmos DbContext for messaging (Outbox + Inbox),
  /// and the EF-based MessageStore + InboxStore.
  /// </summary>
  public static IServiceCollection AddFranzCosmosMessaging(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    // 1️⃣ Bind CosmosOptions
    services.Configure<CosmosOptions>(configuration.GetSection("Cosmos"));

    var cosmos = configuration.GetSection("Cosmos").Get<CosmosOptions>()
                 ?? throw new InvalidOperationException("Cosmos configuration missing.");

    if (!cosmos.Enabled)
      return services;

    // 2️⃣ Register the CosmosMessagingDbContext
    services.AddDbContext<CosmosMessagingDbContext>(options =>
    {
      options.UseCosmos(
          cosmos.AccountEndpoint,
          cosmos.AccountKey,
          cosmos.DatabaseName,
          cosmosOptions =>
          {
            if (!string.IsNullOrWhiteSpace(cosmos.ApplicationName))
            {
              cosmosOptions.HttpClientFactory(() =>
              {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", cosmos.ApplicationName);
                return client;
              });
            }

            if (cosmos.DatabaseThroughput.HasValue)
              cosmosOptions.LimitToEndpoint(cosmos.DatabaseThroughput.Value);
          }
      );
    });

    // 3️⃣ Register Outbox + Inbox Stores
    services.AddScoped<IMessageStore, CosmosEfMessageStore>();
    services.AddScoped<IInboxStore, CosmosEfInboxStore>();

    return services;
  }
}
