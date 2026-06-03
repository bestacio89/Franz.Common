using System.Reflection;
using Franz.Common.AzureCosmosDB.Context;
using Franz.Common.AzureCosmosDB.Messaging;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Extensions;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Franz.Common.EntityFramework.Oracle.Extensions; // New Import
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.Http.EntityFramework.Extensions;
using Franz.Common.MongoDB;
using Franz.Common.MongoDB.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.Extensions;

public static class MultiDatabaseServiceCollectionExtensions
{
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
      case "mariadb" or "postgres" or "sqlserver" or "oracle": // Added oracle
        if (!typeof(DbContextBase).IsAssignableFrom(contextType))
          throw new InvalidOperationException(
              $"Provider '{provider}' requires a context inheriting from DbContextBase, " +
              $"but '{contextType.Name}' does not.");

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

        var addMongo = typeof(MongoServiceCollectionExtensions)
            .GetMethod("AddMongoDbContext")
            ?.MakeGenericMethod(contextType);
        addMongo?.Invoke(null, new object[] { services, section });
        break;

      // --- Cosmos ---
      case "cosmos":
        if (!typeof(CosmosEfMessageStore).IsAssignableFrom(contextType))
          throw new InvalidOperationException(
              $"Provider 'cosmos' requires a context inheriting from AzureCosmosStore, " +
              $"but '{contextType.Name}' does not.");

        var addCosmos = typeof(Franz.Common.AzureCosmosDB.Extensions.CosmosServiceCollectionExtensions)
            .GetMethod("AddFranzCosmosMessaging")
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
      "oracle" => services.AddOracleDatabase<TContext>(config), // Added oracle mapping
      _ => throw new InvalidOperationException($"Unsupported relational provider '{provider}'.")
    };

    services
        .AddDatabaseTransactionPerHttpCall()
        .AddEntityRepositories<TContext>()
        .AddBehaviors();
  }
}