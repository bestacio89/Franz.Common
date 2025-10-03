using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Franz.Common.Messaging.Storage;
using Franz.Common.AzureCosmosDB.Storage;

namespace Franz.Common.AzureCosmosDB.Extensions;

/// <summary>
/// Provides extension methods for registering Cosmos DB in the DI container.
/// </summary>
public static class AzureCosmosDbServiceCollectionExtensions
{
  /// <summary>
  /// Registers Cosmos DB client and database in the DI container.
  /// </summary>
  public static IServiceCollection AddCosmosDatabase(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    var section = configuration.GetSection("CosmosDb");
    var connectionString = section.GetValue<string>("ConnectionString");
    var databaseName = section.GetValue<string>("DatabaseName");

    if (string.IsNullOrWhiteSpace(connectionString))
      throw new InvalidOperationException("Missing 'CosmosDb:ConnectionString'.");
    if (string.IsNullOrWhiteSpace(databaseName))
      throw new InvalidOperationException("Missing 'CosmosDb:DatabaseName'.");

    // CosmosClient should be singleton
    services.AddSingleton<CosmosClient>(_ => new CosmosClient(connectionString));

    // Database reference scoped per request
    services.AddScoped<Database>(sp =>
    {
      var client = sp.GetRequiredService<CosmosClient>();
      return client.GetDatabase(databaseName);
    });

    return services;
  }

  /// <summary>
  /// Registers Cosmos-based message store (Outbox & DeadLetter).
  /// </summary>
  public static IServiceCollection AddCosmosMessageStore(
      this IServiceCollection services,
      string connectionString,
      string dbName,
      string outboxContainer = "OutboxMessages",
      string deadLetterContainer = "DeadLetterMessages")
  {
    var client = new CosmosClient(connectionString);
    var database = client.GetDatabase(dbName);

    services.AddSingleton<IMessageStore>(
        new CosmosDBMessageStore(database, outboxContainer, deadLetterContainer));

    return services;
  }
}
