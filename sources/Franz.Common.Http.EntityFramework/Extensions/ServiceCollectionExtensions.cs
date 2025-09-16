using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Oracle.Extensions;
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.Http.EntityFramework.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{

  public static IServiceCollection AddDatabase<TDbContext>(
    this IServiceCollection services,
    IHostEnvironment hostEnvironment,
    IConfiguration configuration)
    where TDbContext : DbContextBase
  {
    services
        .AddDatabaseTransactionPerHttpCall();

    // Pick database provider
    var provider = configuration["Database:Provider"]?.ToLowerInvariant();

    services = provider switch
    {
      "mariadb" => services.AddMariaDatabase<TDbContext>(configuration),
      "oracle" => services.AddOracleDatabase<TDbContext>(configuration),
      "postgres" => services.AddPostgresDatabase<TDbContext>(configuration),
      "sqlserver" => services.AddSqlServerDatabase<TDbContext>(configuration),
      _ => throw new InvalidOperationException(
                        $"Unsupported DB provider '{provider}'. " +
                        "Use one of: MariaDb, Oracle, Postgres, SqlServer.")
    };

    services
        .AddDatabaseTransactionPerHttpCall()
        .AddGenericRepositories<TDbContext>()
        .AddBehaviors();

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
}
