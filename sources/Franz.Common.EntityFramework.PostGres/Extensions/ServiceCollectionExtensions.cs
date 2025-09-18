using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

          // --- Logging ---
          var actualConnectionString = connectionString; // what EF will really use
          var maskedConnectionString = connectionString.Replace(databaseOptions.Password, "***");

          Console.WriteLine($"[DB] Connection string (masked): {maskedConnectionString}");
          Console.WriteLine($"[DB] Connection string (actual): {actualConnectionString}");
        })
        .AddScoped<DbContextBase>(sp => sp.GetRequiredService<TDbContext>());

    return services;
  }
}
