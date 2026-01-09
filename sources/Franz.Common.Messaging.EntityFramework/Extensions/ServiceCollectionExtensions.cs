using Franz.Common.EntityFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.MongoDB.Extensions;
using Franz.Common.AzureCosmosDB.Extensions;
using Franz.Common.MongoDB;
using Franz.Common.AzureCosmosDB.Context;

namespace Franz.Common.Messaging.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Registers the <typeparamref name="TDbContext"/> with the configured database provider
  /// (MariaDB, Oracle, Postgres, SQL Server, Mongo, or Cosmos).
  /// EF providers get generic repos + behaviors, NoSQL providers get their own bootstrap.
  /// </summary>
  public static IServiceCollection AddDatabase<TDbContext>(
      this IServiceCollection services,
      IHostEnvironment hostEnvironment,
      IConfiguration configuration)
      where TDbContext : DbContextBase
  {
    var provider = configuration["Database:Provider"]?.ToLowerInvariant();

    services = provider switch
    {
      // EF providers (relational)
      "mariadb" => services.AddMariaDatabase<TDbContext>(configuration),

      "postgres" => services.AddPostgresDatabase<TDbContext>(configuration),
      "sqlserver" => services.AddSqlServerDatabase<TDbContext>(configuration),

      // NoSQL providers
      "mongo" => services.AddMongoDbContext<MongoDbContext>(configuration),
      "cosmos" => services.AddFranzCosmosDbContext<CosmosDbContextBase>(configuration),

      _ => throw new InvalidOperationException(
          $"Unsupported DB provider '{provider}'. " +
          "Use one of: MariaDb, Oracle, Postgres, SqlServer, Mongo, Cosmos.")
    };

    // Add EF-specific extras only if it's a relational provider
    if (IsRelational(provider))
    {
      services
          .AddGenericRepositories<TDbContext>()
          .AddBehaviors();
    }

    return services;
  }

  private static bool IsRelational(string? provider)
      => provider is "mariadb" or "oracle" or "postgres" or "sqlserver";
}
