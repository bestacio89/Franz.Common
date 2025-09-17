using Franz.Common.EntityFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Franz.Common.EntityFramework.MariaDB.Extensions;
using Franz.Common.EntityFramework.SQLServer.Extensions;
using Franz.Common.EntityFramework.Postgres.Extensions;
using Franz.Common.EntityFramework.Oracle.Extensions;


  

namespace Franz.Common.Messaging.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Registers the <typeparamref name="TDbContext"/> with the configured database provider
  /// (MariaDB, Oracle, Postgres, or SQL Server), plus Franz generic repositories and behaviors.
  /// </summary>
  public static IServiceCollection AddDatabase<TDbContext>(
      this IServiceCollection services,
      IHostEnvironment hostEnvironment,
      IConfiguration configuration)
      where TDbContext : DbContextBase
  {
    // Transaction filter for per-request consistency
   

    // Resolve provider from config
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

    // Common add-ons
    services
        .AddGenericRepositories<TDbContext>()
        .AddBehaviors();

    return services;
  }
}
