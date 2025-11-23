using Franz.Common.AzureCosmosDB;
using Franz.Common.AzureCosmosDB.Extensions;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.Http.EntityFramework.Extensions;
using Franz.Common.Http.EntityFramework.Transactions;
using Franz.Common.MongoDB;
using Franz.Common.MongoDB.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;


public static class MultiDatabaseServiceCollectionExtensions
{
  /// <summary>
  /// Registers multiple databases in a flexible way.
  /// Each context can be EF (relational), Mongo, or Cosmos.
  /// </summary>
 public static void RegisterDatabaseForContext<TContext>(
    IServiceCollection services,
    IConfiguration section)
    where TContext : class
  {
    var provider = section.GetValue<string>("Provider")?.ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(provider))
      return;

    var contextType = typeof(TContext);

    switch (provider)
    {
      // --- EF Relational ---
      case "mariadb" or "postgres" or "sqlserver":
        if (!typeof(DbContextBase).IsAssignableFrom(contextType))
          throw new InvalidOperationException(
              $"Provider '{provider}' requires a context inheriting from DbContextBase, " +
              $"but '{contextType.Name}' does not.");

        // Call CallRelationalRegistration<TContext>() via reflection
        var callRelational = typeof(MultiDatabaseServiceCollectionExtensions)
            .GetMethod(nameof(CallRelationalRegistration),
                BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(contextType);

        callRelational.Invoke(null, new object[] { services, provider, section });
        break;

      // --- Mongo ---
      case "mongo":
        if (!typeof(MongoDbContext).IsAssignableFrom(contextType))
          throw new InvalidOperationException(
              $"Provider 'mongo' requires a context inheriting from MongoDbContext, " +
              $"but '{contextType.Name}' does not.");

        // Safe cast through reflection to avoid CS0311
        var addMongo = typeof(MongoServiceCollectionExtensions)
            .GetMethod("AddMongoDbContext")
            ?.MakeGenericMethod(contextType);
        addMongo?.Invoke(null, new object[] { services, section });
        break;

      // --- Cosmos ---
      case "cosmos":
        if (!typeof(AzureCosmosStore).IsAssignableFrom(contextType))
          throw new InvalidOperationException(
              $"Provider 'cosmos' requires a context inheriting from AzureCosmosStore, " +
              $"but '{contextType.Name}' does not.");

        var addCosmos = typeof(AzureCosmosDbServiceCollectionExtensions)
            .GetMethod("AddCosmosStore")
            ?.MakeGenericMethod(contextType);
        addCosmos?.Invoke(null, new object[] { services, section });
        break;

      default:
        throw new InvalidOperationException($"Unsupported provider '{provider}'.");
    }
  }

  private static void CallRelationalRegistration<TContext>(
      IServiceCollection services,
      string provider,
      IConfiguration config)
      where TContext : DbContextBase
  {
    services = provider switch
    {
      "mariadb" => services.AddMariaDatabase<TContext>(config),
      "postgres" => services.AddPostgresDatabase<TContext>(config),
      "sqlserver" => services.AddSqlServerDatabase<TContext>(config),
      _ => throw new InvalidOperationException($"Unsupported relational provider '{provider}'.")
    };

    services
        .AddDatabaseTransactionPerHttpCall()
        .AddGenericRepositories<TContext>()
        .AddBehaviors();
  }


 
}

