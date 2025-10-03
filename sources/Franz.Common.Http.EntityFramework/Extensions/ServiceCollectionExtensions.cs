using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Franz.Common.EntityFramework.Oracle.Extensions;
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.Http.EntityFramework.Transactions;
using Franz.Common.MongoDB;
using Franz.Common.MongoDB.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Franz.Common.AzureCosmosDB.Extensions;

namespace Franz.Common.Http.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddDatabase<TDbContext>(
      this IServiceCollection services,
      IHostEnvironment hostEnvironment,
      IConfiguration configuration)
      where TDbContext : DbContextBase
  {
    // Always wire the transaction filter by default
    services.AddDatabaseTransactionPerHttpCall();

    // Pick database provider
    var provider = configuration["Database:Provider"]?.ToLowerInvariant();

    services = provider switch
    {
      // Relational EF providers
      "mariadb" => services.AddMariaDatabase<TDbContext>(configuration),
      "oracle" => services.AddOracleDatabase<TDbContext>(configuration),
      "postgres" => services.AddPostgresDatabase<TDbContext>(configuration),
      "sqlserver" => services.AddSqlServerDatabase<TDbContext>(configuration),

      // NoSQL providers
      "mongo" => services.AddMongoDbContext<MongoDbContext>(configuration),
      "cosmos" => services.AddCosmosDatabase(configuration),

      _ => throw new InvalidOperationException(
              $"Unsupported DB provider '{provider}'. " +
              "Valid: MariaDb, Oracle, Postgres, SqlServer, Mongo, Cosmos.")
    };

    // Only EF-based providers support DbContext behaviors
    if (IsRelational(provider))
    {
      services
          .AddDatabaseTransactionPerHttpCall()
          .AddGenericRepositories<TDbContext>()
          .AddBehaviors();
    }

    return services;
  }

  public static IServiceCollection AddDatabaseTransactionPerHttpCall(this IServiceCollection services)
  {
    services
        .AddNoDuplicateScoped<TransactionFilter>()
        .AddMvc()
        .AddMvcOptions(setup =>
        {
          setup.Filters.AddService<TransactionFilter>();
        });

    return services;
  }

  private static bool IsRelational(string? provider)
  {
    return provider is "mariadb" or "oracle" or "postgres" or "sqlserver";
  }

  public static IServiceCollection AddDatabases<TDbContext>(
    this IServiceCollection services,
    IHostEnvironment env,
    IConfiguration config)
    where TDbContext : DbContextBase
  {
    // Relational
    var relationalSection = config.GetSection("Databases:Relational");
    var relationalProvider = relationalSection.GetValue<string>("Provider")?.ToLowerInvariant();

    if (!string.IsNullOrWhiteSpace(relationalProvider))
    {
      services = relationalProvider switch
      {
        "mariadb" => services.AddMariaDatabase<TDbContext>(config),
        "oracle" => services.AddOracleDatabase<TDbContext>(config),
        "postgres" => services.AddPostgresDatabase<TDbContext>(config),
        "sqlserver" => services.AddSqlServerDatabase<TDbContext>(config),
        _ => throw new InvalidOperationException($"Unsupported relational provider '{relationalProvider}'")
      };

      services
          .AddDatabaseTransactionPerHttpCall()
          .AddGenericRepositories<TDbContext>()
          .AddBehaviors();
    }

    // Document (NoSQL)
    var documentSection = config.GetSection("Databases:Document");
    var documentProvider = documentSection.GetValue<string>("Provider")?.ToLowerInvariant();

    if (!string.IsNullOrWhiteSpace(documentProvider))
    {
      services = documentProvider switch
      {
        "mongo" => services.AddMongoDbContext<MongoDbContext>(config),
        "cosmos" => services.AddCosmosDatabase(config),
        _ => throw new InvalidOperationException($"Unsupported document provider '{documentProvider}'")
      };
    }

    return services;
  }

}
