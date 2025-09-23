using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Franz.Common.EntityFramework.MariaDB.Extensions;

public static class ServiceCollectionExtensions
{
  private const string DefaultServerName = "localhost";
  private const string DefaultDatabaseName = "library";
  private const string DefaultUserName = "root";
  private const string DefaultPassword = "password";
  private const int DefaultPort = 3306; // real MariaDB default
  private const string DefaultSslMode = "None";

  public static IServiceCollection AddMariaDatabase<TDbContext>(
      this IServiceCollection services,
      IConfiguration configuration)
      where TDbContext : DbContextBase
  {
    services
        .AddDatabaseOptions(configuration)
        .AddDbContext<TDbContext>((serviceProvider, dbContextBuilder) =>
        {
          var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
          var logger = serviceProvider.GetRequiredService<ILogger<TDbContext>>();

          var mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder
          {
            Server = string.IsNullOrWhiteSpace(databaseOptions.ServerName) ? DefaultServerName : databaseOptions.ServerName,
            Database = string.IsNullOrWhiteSpace(databaseOptions.DatabaseName) ? DefaultDatabaseName : databaseOptions.DatabaseName,
            UserID = string.IsNullOrWhiteSpace(databaseOptions.UserName) ? DefaultUserName : databaseOptions.UserName,
            Password = string.IsNullOrWhiteSpace(databaseOptions.Password) ? DefaultPassword : databaseOptions.Password,
            Port = (uint)(databaseOptions.Port > 0 ? databaseOptions.Port : DefaultPort),
            SslMode = Enum.Parse<MySqlSslMode>(
                              string.IsNullOrWhiteSpace(databaseOptions.SslMode)
                                  ? DefaultSslMode
                                  : databaseOptions.SslMode,
                              ignoreCase: true)
          };

          var connectionString = mySqlConnectionStringBuilder.ConnectionString;

          // Multi-tenant substitution
          var domainContextAccessor = serviceProvider.GetService<IDomainContextAccessor>();
          var domainId = domainContextAccessor?.GetCurrentDomainId();

          connectionString = domainId.HasValue
                  ? connectionString.Replace("{dbName}", domainId.Value.ToString())
                  : connectionString.Replace($"_{{dbName}}", string.Empty);

          // Force MariaDB 11.4
          dbContextBuilder.UseMySql(
              connectionString,
              ServerVersion.Create(new Version(11, 4, 0), ServerType.MariaDb));

          dbContextBuilder.EnableSensitiveDataLogging();

          // Mask password safely
          var masked = !string.IsNullOrEmpty(databaseOptions.Password)
              ? connectionString.Replace(databaseOptions.Password, "***")
              : connectionString;

          logger.LogDebug("[DB] Using connection string: {ConnectionString}", masked);
        })
        .AddScoped<DbContextBase>(sp => sp.GetRequiredService<TDbContext>());

    return services;
  }
}
