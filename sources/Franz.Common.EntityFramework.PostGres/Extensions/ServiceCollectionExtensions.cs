using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.EntityFramework.Extensions;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Franz.Common.EntityFramework.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DefaultServerName = "localhost";
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

                var databaseName = DatabaseTargetResolver.ResolveDatabaseName(
                    databaseOptions,
                    serviceProvider.GetService<ITenantExecutionContextAccessor>(),
                    serviceProvider.GetService<IDomainContextAccessor>());

                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = string.IsNullOrWhiteSpace(databaseOptions.ServerName) ? DefaultServerName : databaseOptions.ServerName,
                    Database = databaseName,
                    Username = string.IsNullOrWhiteSpace(databaseOptions.UserName) ? DefaultUserName : databaseOptions.UserName,
                    Password = string.IsNullOrWhiteSpace(databaseOptions.Password) ? DefaultPassword : databaseOptions.Password,
                    Port = (int)(databaseOptions.Port > 0 ? databaseOptions.Port.Value : DefaultPort),
                    SslMode = Enum.Parse<SslMode>(
                        string.IsNullOrWhiteSpace(databaseOptions.SslMode) ? SslDefaultMode : databaseOptions.SslMode,
                        ignoreCase: true)
                };

                var connectionString = builder.ConnectionString;
                dbContextBuilder.UseNpgsql(connectionString);
                if (databaseOptions.EnableSensitiveDataLogging) dbContextBuilder.EnableSensitiveDataLogging();

                var masked = !string.IsNullOrEmpty(databaseOptions.Password)
                    ? connectionString.Replace(databaseOptions.Password, "***")
                    : connectionString;
                logger.LogDebug("[DB] Connection string (masked): {ConnectionString}", masked);
            })
            .AddScoped<DbContextBase>(sp => sp.GetRequiredService<TDbContext>());

        return services;
    }
}
