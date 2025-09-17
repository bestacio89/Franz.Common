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
  private const string DefaultUserName = "root";
  private const string DefaultPassword = "password";
  private const string SslDefaultMode = "Preferred";

  public static IServiceCollection AddPostgresDatabase<TDbContext>(
    this IServiceCollection services,
    IConfiguration configuration)
    where TDbContext : DbContextBase
  {
    services
        .AddDatabaseOptions(configuration)
        .AddDbContext<TDbContext>((serviceProvider, dbContextBuilder) =>
        {
          var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();

          var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder
          {
            Host = databaseOptions.Value.ServerName ?? DefaultServerName,
            Database = databaseOptions.Value.DatabaseName ?? DatabaseNamePattern,
            Username = databaseOptions.Value.UserName ?? "postgres",
            Password = databaseOptions.Value.Password ?? DefaultPassword,
            Port = (int)(databaseOptions.Value.Port ?? 5432),
            SslMode = SslMode.Disable // or Prefer, depending on your setup
          };

          var connectionString = npgsqlConnectionStringBuilder.ConnectionString;

          var domainContextAccessor = serviceProvider.GetService<IDomainContextAccessor>();
          var domainId = domainContextAccessor?.GetCurrentDomainId();

          connectionString = domainId.HasValue
              ? connectionString.Replace(DatabaseNamePattern, domainId.Value.ToString())
              : connectionString.Replace($"_{DatabaseNamePattern}", string.Empty);

          dbContextBuilder.UseNpgsql(connectionString);
          dbContextBuilder.EnableSensitiveDataLogging();
        })
        .AddScoped<DbContextBase>(sp => sp.GetRequiredService<TDbContext>());

    return services;
  }

}

