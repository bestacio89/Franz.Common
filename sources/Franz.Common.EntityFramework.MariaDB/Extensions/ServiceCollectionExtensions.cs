using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  private const string DefaultServerName = "localhost";
  private const string DatabaseNamePattern = "{dbName}";
  private const string DefaultUserName = "root";
  private const string DefaultPassword = "password";
  private const string SslDefaultMode = "Preferred";

  public static IServiceCollection AddMariaDatabase<TDbContext>(this IServiceCollection services, IConfiguration configuration)
    where TDbContext : DbContextBase
  {
    services
      .AddDatabaseOptions(configuration)
      .AddDbContext<TDbContext>((serviceProvider, dbContextBuilder) =>
      {
        var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();

        var mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder
        {
          Server = databaseOptions.Value.ServerName ?? DefaultServerName,
          Database = databaseOptions.Value.DatabaseName ?? DatabaseNamePattern,
          UserID = databaseOptions.Value.UserName ?? DefaultUserName,
          Password = databaseOptions.Value.Password ?? DefaultPassword,
          Port = databaseOptions.Value.Port ?? 3308,
          SslMode = Enum.Parse<MySqlSslMode>(databaseOptions.Value.SslMode ?? SslDefaultMode, true),
        };

        var connectionString = mySqlConnectionStringBuilder.ConnectionString;

        var domainContextAccessor = serviceProvider.GetService<IDomainContextAccessor>();

        var domainId = domainContextAccessor?.GetCurrentDomainId();
        connectionString = domainId.HasValue
          ? connectionString.Replace(DatabaseNamePattern, domainId!.Value.ToString())
          : connectionString.Replace($"_{DatabaseNamePattern}", string.Empty);

        dbContextBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        dbContextBuilder.EnableSensitiveDataLogging();
      })
      .AddScoped<DbContextBase>(serviceProvider => serviceProvider.GetRequiredService<TDbContext>());

    return services;
  }
}
