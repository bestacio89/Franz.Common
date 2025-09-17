using Franz.Common.EntityFramework.Configuration;
using Franz.Common.EntityFramework;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Franz.Common.EntityFramework.SQLServer.Enums;

namespace Franz.Common.EntityFramework.SQLServer.Extensions;

public static class ServiceCollectionExtensions
{
  private const string DefaultServerName = "localhost";
  private const string DatabaseNamePattern = "{dbName}";
  private const string DefaultUserName = "sa";
  private const string DefaultPassword = "password";
  private const int DefaultPort = 1433;
  private const string SslDefaultMode = "Preferred";

  public static IServiceCollection AddSqlServerDatabase<TDbContext>(
      this IServiceCollection services,
      IConfiguration configuration)
      where TDbContext : DbContextBase
  {
    services
        .AddDatabaseOptions(configuration)
        .AddDbContext<TDbContext>((serviceProvider, dbContextBuilder) =>
        {
          var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();

          var serverName = databaseOptions.Value.ServerName ?? DefaultServerName;
          var port = databaseOptions.Value.Port > 0 ? databaseOptions.Value.Port.Value : DefaultPort;

          var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
          {
            DataSource = $"{serverName},{port}",
            InitialCatalog = databaseOptions.Value.DatabaseName ?? DatabaseNamePattern,
            UserID = databaseOptions.Value.UserName ?? DefaultUserName,
            Password = databaseOptions.Value.Password ?? DefaultPassword,
            Encrypt = GetMap(Enum.Parse<SslEnforcement>(databaseOptions.Value.SslMode ?? SslDefaultMode, true))
          };

          var connectionString = sqlConnectionStringBuilder.ConnectionString;

          var domainContextAccessor = serviceProvider.GetService<IDomainContextAccessor>();
          var domainId = domainContextAccessor?.GetCurrentDomainId();

          connectionString = domainId.HasValue
                  ? connectionString.Replace(DatabaseNamePattern, domainId.Value.ToString())
                  : connectionString.Replace($"_{DatabaseNamePattern}", string.Empty);

          dbContextBuilder.UseSqlServer(connectionString);
          dbContextBuilder.EnableSensitiveDataLogging();
        })
        .AddScoped<DbContextBase>(sp => sp.GetRequiredService<TDbContext>());

    return services;
  }

  private static SqlConnectionEncryptOption GetMap(SslEnforcement encrypt)
  {
    return encrypt switch
    {
      SslEnforcement.Disabled => SqlConnectionEncryptOption.Optional,
      SslEnforcement.Preferred => SqlConnectionEncryptOption.Mandatory,
      SslEnforcement.Enabled => SqlConnectionEncryptOption.Strict,
      _ => SqlConnectionEncryptOption.Optional
    };
  }
}

