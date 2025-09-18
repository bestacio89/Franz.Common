using Franz.Common.EntityFramework.Configuration;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace Franz.Common.EntityFramework.Oracle.Extensions;

public static class ServiceCollectionExtensions
{
  private const string DefaultServerName = "localhost";
  private const string DatabaseNamePattern = "{dbName}";
  private const string DefaultUserName = "system";
  private const string DefaultPassword = "oracle";
  private const int DefaultPort = 1521;

  public static IServiceCollection AddOracleDatabase<TDbContext>(
      this IServiceCollection services,
      IConfiguration configuration)
      where TDbContext : DbContextBase
  {
    services
        .AddDatabaseOptions(configuration)
        .AddDbContext<TDbContext>((serviceProvider, dbContextBuilder) =>
        {
          var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();

          // Build proper DataSource string: host:port/serviceName
          var host = databaseOptions.Value.ServerName ?? DefaultServerName;
          var port = databaseOptions.Value.Port > 0 ? databaseOptions.Value.Port.Value : DefaultPort;
          var serviceName = databaseOptions.Value.DatabaseName ?? DatabaseNamePattern;

          var oracleConnectionStringBuilder = new OracleConnectionStringBuilder
          {
            DataSource = $"{host}:{port}/{serviceName}",
            UserID = databaseOptions.Value.UserName ?? DefaultUserName,
            Password = databaseOptions.Value.Password ?? DefaultPassword
          };

          var connectionString = oracleConnectionStringBuilder.ConnectionString;

          // Apply domain replacement if needed
          var domainContextAccessor = serviceProvider.GetService<IDomainContextAccessor>();
          var domainId = domainContextAccessor?.GetCurrentDomainId();

          connectionString = domainId.HasValue
                  ? connectionString.Replace(DatabaseNamePattern, domainId.Value.ToString())
                  : connectionString.Replace($"_{DatabaseNamePattern}", string.Empty);

          dbContextBuilder.UseOracle(connectionString);

          dbContextBuilder.EnableSensitiveDataLogging();
        })
        .AddScoped<DbContextBase>(sp => sp.GetRequiredService<TDbContext>());

    return services;
  }
}
