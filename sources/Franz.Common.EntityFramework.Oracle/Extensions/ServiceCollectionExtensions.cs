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
  private const string DefaultUserName = "root";
  private const string DefaultPassword = "password";
  private const string SslDefaultMode = "Preferred";

  public static IServiceCollection AddOracleDatabase<TDbContext>(this IServiceCollection services, IConfiguration configuration)
      where TDbContext : DbContextBase
  {
    services
      .AddDatabaseOptions(configuration)
      .AddDbContext<TDbContext>((serviceProvider, dbContextBuilder) =>
      {
        var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();
        var oracleConnectionStringBuilder = new OracleConnectionStringBuilder
        {
          DataSource = databaseOptions.Value.ServerName ?? DefaultServerName,
          UserID = databaseOptions.Value.UserName ?? DefaultUserName,
          Password = databaseOptions.Value.Password ?? DefaultPassword,
          //Encryption options if there any 
        };

        var connectionString = oracleConnectionStringBuilder.ConnectionString;

        var domainContextAccessor = serviceProvider.GetService<IDomainContextAccessor>();

        var domainId = domainContextAccessor?.GetCurrentDomainId();
        connectionString = domainId.HasValue
                  ? connectionString.Replace(DatabaseNamePattern, domainId!.Value.ToString())
                  : connectionString.Replace($"_{DatabaseNamePattern}", string.Empty);

        dbContextBuilder.UseOracle(connectionString);

        dbContextBuilder.EnableSensitiveDataLogging();
      })
      .AddScoped<DbContextBase>(serviceProvider => serviceProvider.GetRequiredService<TDbContext>());

    return services;

  }

}

