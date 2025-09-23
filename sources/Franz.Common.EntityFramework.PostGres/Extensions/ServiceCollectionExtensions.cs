using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.MultiTenancy;
using Npgsql;

namespace Franz.Common.EntityFramework.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
  private const string DefaultServerName = "localhost";
  private const string DatabaseNamePattern = "{dbName}";
  private const string DefaultUserName = "postgres";
  private const string DefaultPassword = "password";
  private const int DefaultPort = 5432;
  private const string SslDefaultMode = "Disable";

  public static IServiceCollection AddPostgresDatabase<TDbContext>(
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

          var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder
          {
            Host = string.IsNullOrWhiteSpace(databaseOptions.ServerName) ? DefaultServerName : databaseOptions.ServerName,
            Database = string.IsNullOrWhiteSpace(databaseOptions.DatabaseName) ? DatabaseNamePattern : databaseOptions.DatabaseName,
            Username = string.IsNullOrWhiteSpace(databaseOptions.UserName) ? DefaultUserName : databaseOptions.UserName,
            Password = string.IsNullOrWhiteSpace(databaseOptions.Password) ? DefaultPassword : databaseOptions.Password,
            Port = (int)(databaseOptions.Port > 0 ? databaseOptions.Port.Value : DefaultPort),
            SslMode = Enum.Parse<SslMode>(
                        string.IsNullOrWhiteSpace(databaseOptions.SslMode)
                            ? SslDefaultMode
                            : databaseOptions.SslMode,
                        ignoreCase: true)
          };

          var connectionString = npgsqlConnectionStringBuilder.ConnectionString;

          // Apply domain replacement if needed
          var domainContextAccessor = serviceProvider.GetService<IDomainContextAccessor>();
          var domainId = domainContextAccessor?.GetCurrentDomainId();

          connectionString = domainId.HasValue
                  ? connectionString.Replace(DatabaseNamePattern, domainId.Value.ToString())
                  : connectionString.Replace($"_{DatabaseNamePattern}", string.Empty);

          dbContextBuilder.UseNpgsql(connectionString);
          dbContextBuilder.EnableSensitiveDataLogging();

          // Mask password safely (no warnings!)
          var masked = !string.IsNullOrEmpty(databaseOptions.Password)
              ? connectionString.Replace(databaseOptions.Password, "***")
              : connectionString;

          logger.LogDebug("[DB] Connection string (masked): {ConnectionString}", masked);
        })
        .AddScoped<DbContextBase>(sp => sp.GetRequiredService<TDbContext>());

    return services;
  }
}
