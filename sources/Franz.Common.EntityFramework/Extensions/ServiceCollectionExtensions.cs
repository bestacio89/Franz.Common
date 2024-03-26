using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Behaviors;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.EntityFramework.Properties;
using Franz.Common.EntityFramework.Repositories;
using Franz.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  private const string DatabaseSectionName = "Database";

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddDatabaseOptions(this IServiceCollection services, IConfiguration? configuration)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var configurationSection = configuration?.GetSection(DatabaseSectionName);
    var hasMessagingConnectionOptions = services.Any(service => service.ServiceType == typeof(IConfigureOptions<DatabaseOptions>));

    if (configurationSection?.Exists() == true && !hasMessagingConnectionOptions)
    {
      services
        .AddOptions()
        .Configure<DatabaseOptions>(configurationSection);
    }
    else if (!hasMessagingConnectionOptions)
    {
      throw new TechnicalException(Resources.DatabaseNoConfigurationException);
    }

    return services;
  }

  public static IServiceCollection AddGenericRepositories<TDbContext>(this IServiceCollection services)
    where TDbContext : DbContext
  {
    services = services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

    var typeDbContext = typeof(TDbContext);
    var entityTypes = GetEntityTypesFromDbContext(typeDbContext);

    var repositoryInterface = typeof(IReadRepository<>);
    var repositoryImplementation = typeof(ReadRepository<>);

    foreach (var entityType in entityTypes)
    {
      var genericRepositoryType = repositoryInterface.MakeGenericType(entityType);
      if (!services.Any(x => x.ImplementationType == genericRepositoryType))
      {
        var implementationType = repositoryImplementation.MakeGenericType(entityType);

        services.AddNoDuplicateScoped(genericRepositoryType, implementationType);
      }
    }

    return services;
  }

  private static IEnumerable<Type> GetEntityTypesFromDbContext(Type typeDbContext)
  {
    var results = typeDbContext.GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(property => property.PropertyType.IsGenericType)
      .Where(property => property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
      .Select(property => property.PropertyType.GetGenericArguments().Single())
      .Where(type => type.Implements<IEntity>())
      .ToList();

    return results;
  }

  public static IServiceCollection AddBehaviors(this IServiceCollection services)
  {
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PersistenceBehavior<,>));

    return services;
  }
}
