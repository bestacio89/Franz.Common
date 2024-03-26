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
  private const string DefaultUserName = "root";
  private const string DefaultPassword = "password";
  private const string SslDefaultMode = "Preferred";

  public static IServiceCollection AddSqlServerDatabase<TDbContext>(this IServiceCollection services, IConfiguration configuration)
      where TDbContext : DbContextBase
  {
    services
      .AddDatabaseOptions(configuration)
      .AddDbContext<TDbContext>((serviceProvider, dbContextBuilder) =>
      {
        var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();

        var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
        {
          DataSource = databaseOptions.Value.ServerName ?? DefaultServerName,
          InitialCatalog = databaseOptions.Value.DatabaseName ?? DatabaseNamePattern,
          UserID = databaseOptions.Value.UserName ?? DefaultUserName,
          Password = databaseOptions.Value.Password ?? DefaultPassword,
          Encrypt = GetMap(Enum.Parse<SslEnforcement>(databaseOptions.Value.SslMode ?? SslDefaultMode, true))

        };

        var connectionString = sqlConnectionStringBuilder.ConnectionString;

        var domainContextAccessor = serviceProvider.GetService<IDomainContextAccessor>();

        var domainId = domainContextAccessor?.GetCurrentId();
        connectionString = domainId.HasValue
                  ? connectionString.Replace(DatabaseNamePattern, domainId!.Value.ToString())
                  : connectionString.Replace($"_{DatabaseNamePattern}", string.Empty);

        dbContextBuilder.UseSqlServer(connectionString);

        dbContextBuilder.EnableSensitiveDataLogging();
      })
      .AddScoped<DbContextBase>(serviceProvider => serviceProvider.GetRequiredService<TDbContext>());

    return services;
  
  }
  private static SqlConnectionEncryptOption GetMap(SslEnforcement encrypt)
  {
    
    if (encrypt == SslEnforcement.Enabled)
      return SqlConnectionEncryptOption.Mandatory;
    else if (encrypt == SslEnforcement.Preferred)
      return SqlConnectionEncryptOption.Strict;
    else if (encrypt == SslEnforcement.Disabled)
      return SqlConnectionEncryptOption.Optional;
    else
      return SqlConnectionEncryptOption.Optional;
  }
}

