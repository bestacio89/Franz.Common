using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.Http.EntityFramework.Transactions;
using Franz.Common.MongoDB;
using Franz.Common.MongoDB.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Franz.Common.AzureCosmosDB;
using Franz.Common.AzureCosmosDB.Extensions;
namespace Franz.Common.Http.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddRelationalDatabase<TDbContext>(
     this IServiceCollection services,
     IHostEnvironment env,
     IConfiguration config)
     where TDbContext : DbContextBase
  {
    var provider = config["Database:Provider"]?.ToLowerInvariant()
        ?? throw new InvalidOperationException("Missing relational provider.");

    services = provider switch
    {
      "mariadb" => services.AddMariaDatabase<TDbContext>(config),
      "postgres" => services.AddPostgresDatabase<TDbContext>(config),
      "sqlserver" => services.AddSqlServerDatabase<TDbContext>(config),
      _ => throw new InvalidOperationException($"Unsupported relational provider '{provider}'.")
    };

    return services
        .AddDatabaseTransactionPerHttpCall()
        .AddGenericRepositories<TDbContext>()
        .AddBehaviors();
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

  public static IServiceCollection AddMongoDatabase<TMongoContext>(
    this IServiceCollection services,
    IConfiguration config)
    where TMongoContext : MongoDbContext
  {
    var provider = config["Database:Provider"]?.ToLowerInvariant();
    if (provider != "mongo")
      throw new InvalidOperationException("MongoDbContext requires provider 'mongo'.");

    return services.AddMongoDbContext<TMongoContext>(config);
  }

  public static IServiceCollection AddCosmosDatabase<TCosmosContext>(
    this IServiceCollection services,
    IConfiguration config)
    where TCosmosContext : AzureCosmosStore
  {
    var provider = config["Database:Provider"]?.ToLowerInvariant();
    if (provider != "cosmos")
      throw new InvalidOperationException("AzureCosmosStore requires provider 'cosmos'.");

    return services.AddCosmosDatabase(config);
  }




  

}
