using Franz.Common.Messaging.Storage;
using Franz.Common.MongoDB.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Franz.Common.MongoDB.Extensions;

/// <summary>
/// Provides extension methods for registering MongoDB contexts in the DI container.
/// </summary>
public static class MongoServiceCollectionExtensions
{
  /// <summary>
  /// Registers a MongoDB context of type <typeparamref name="TContext"/> in the service collection.
  /// </summary>
  /// <typeparam name="TContext">The MongoDB context type derived from <see cref="MongoDbContext"/>.</typeparam>
  /// <param name="services">The service collection.</param>
  /// <param name="configuration">The application configuration containing the "MongoDb" section.</param>
  /// <returns>The modified service collection.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if required configuration values (ConnectionString or DatabaseName) are missing.
  /// </exception>
  public static IServiceCollection AddMongoDbContext<TContext>(
      this IServiceCollection services,
      IConfiguration configuration)
      where TContext : MongoDbContext
  {
    var mongoDbSection = configuration.GetSection("MongoDb");
    var connectionString = mongoDbSection.GetValue<string>("ConnectionString");
    var databaseName = mongoDbSection.GetValue<string>("DatabaseName");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
      throw new InvalidOperationException("MongoDB connection string is not configured. Please set 'MongoDb:ConnectionString' in configuration.");
    }

    if (string.IsNullOrWhiteSpace(databaseName))
    {
      throw new InvalidOperationException("MongoDB database name is not configured. Please set 'MongoDb:DatabaseName' in configuration.");
    }

    // Register MongoClient as a singleton (thread-safe, recommended by MongoDB docs)
    services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));

    // Register IMongoDatabase as scoped per request
    services.AddScoped<IMongoDatabase>(sp =>
    {
      var client = sp.GetRequiredService<IMongoClient>();
      return client.GetDatabase(databaseName);
    });

    // Register the custom context
    services.AddScoped<TContext>();

    return services;
  }

  public static IServiceCollection AddMongoMessageStore(
    this IServiceCollection services,
    string connectionString,
    string dbName,
    string outboxCollectionName = "OutboxMessages",
    string deadLetterCollectionName = "DeadLetterMessages")
  {
    var client = new MongoClient(connectionString);
    var database = client.GetDatabase(dbName);

    // Infrastructure primitives
    services.AddSingleton<IMongoClient>(client);
    services.AddSingleton<IMongoDatabase>(database);

    // Messaging persistence
    services.AddSingleton<IMessageStore>(
      new MongoMessageStore(database, outboxCollectionName, deadLetterCollectionName));

    services.AddSingleton<IInboxStore, MongoInboxStore>();

    return services;
  }

}
