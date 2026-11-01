using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.EntityFramework.Extensions;
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

          var host = databaseOptions.Value.ServerName ?? DefaultServerName;
          var port = databaseOptions.Value.Port > 0 ? databaseOptions.Value.Port.Value : DefaultPort;
          var serviceName = DatabaseTargetResolver.ResolveDatabaseName(
              databaseOptions.Value,
              serviceProvider.GetService<ITenantExecutionContextAccessor>(),
              serviceProvider.GetService<IDomainContextAccessor>());

          var oracleConnectionStringBuilder = new OracleConnectionStringBuilder
          {
            DataSource = $"{host}:{port}/{serviceName}",
            UserID = databaseOptions.Value.UserName ?? DefaultUserName,
            Password = databaseOptions.Value.Password ?? DefaultPassword
          };

          dbContextBuilder.UseOracle(oracleConnectionStringBuilder.ConnectionString);

          if (databaseOptions.Value.EnableSensitiveDataLogging)
            dbContextBuilder.EnableSensitiveDataLogging();
        })
        .AddScoped<DbContextBase>(sp => sp.GetRequiredService<TDbContext>());

    return services;
  }
}
