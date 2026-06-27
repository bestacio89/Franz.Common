using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Business.Repositories;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.EntityFramework.Behaviors;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.EntityFramework.Properties;
using Franz.Common.EntityFramework.Repositories;
using Franz.Common.Errors;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Franz.Common.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
  private const string DatabaseSectionName = "Database";

  #region DATABASE OPTIONS

  public static IServiceCollection AddDatabaseOptions(
      this IServiceCollection services,
      IConfiguration? configuration)
  {
    var section = configuration?.GetSection(DatabaseSectionName);

    var alreadyConfigured = services.Any(s =>
        s.ServiceType == typeof(IConfigureOptions<DatabaseOptions>));

    if (section?.Exists() == true && !alreadyConfigured)
    {
      services.AddOptions().Configure<DatabaseOptions>(section);
    }
    else if (!alreadyConfigured)
    {
      throw new TechnicalException(Resources.DatabaseNoConfigurationException);
    }

    return services;
  }

  #endregion



  #region ENTITY REPOSITORIES (FULL CRUD MODEL)

  public static IServiceCollection AddEntityRepositories<TDbContext>(
    this IServiceCollection services)
    where TDbContext : DbContextBase
  {
    services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

    var entityTypes = GetEfEntityTypes(typeof(TDbContext));

    foreach (var entityType in entityTypes)
    {
      // =========================================================
      // 1. Resolve IEntity<TKey>
      // =========================================================
      var entityInterface = entityType
          .GetInterfaces()
          .FirstOrDefault(i =>
              i.IsGenericType &&
              i.GetGenericTypeDefinition() == typeof(IEntity<>));

      if (entityInterface is null)
        throw new InvalidOperationException(
            $"Entity {entityType.Name} does not implement IEntity<TKey>");

      var keyType = entityInterface.GetGenericArguments()[0];

      // =========================================================
      // 2. Build repository types (FIXED ARITY)
      // =========================================================
      var serviceType = typeof(IEntityRepository<,>)
          .MakeGenericType(entityType, keyType);

      var implementationType = typeof(EntityRepository<,,>)
          .MakeGenericType(typeof(TDbContext), entityType, keyType);

      // =========================================================
      // 3. Register safely
      // =========================================================
      services.AddNoDuplicateScoped(serviceType, implementationType);
    }

    return services;
  }

  #endregion

  #region AGGREGATES (EXPLICIT ONLY — DO NOT AUTO-SCAN)

  /// <summary>
  /// Aggregates are event-sourced and MUST be explicitly registered.
  /// No reflection scanning is allowed for aggregates.
  /// </summary>
  public static IServiceCollection AddAggregateRepository<
      TAggregate,
      TEvent,
      TId,
      TRepository>(
      this IServiceCollection services)
      where TAggregate : class, IAggregateRoot<TEvent>
      where TEvent : class, IDomainEvent
      where TRepository : class, IAggregateRootRepository<TAggregate, TEvent, TId>
  {
    services.AddScoped<
        IAggregateRootRepository<TAggregate, TEvent, TId>,
        TRepository>();

    services.AddScoped<TRepository>();

    return services;
  }

  #endregion

  #region EF ENTITY DISCOVERY (HARDENED)
  private static bool ImplementsEntityInterface(Type type)
  {
    return type.GetInterfaces()
        .Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntity<>));
  }
  private static IEnumerable<Type> GetEfEntityTypes(Type dbContextType)
  {
    return dbContextType
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.PropertyType.IsGenericType)
      .Where(p => p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
      .Select(p => p.PropertyType.GetGenericArguments().Single())

      // =========================================================
      // Only persistence entities implementing IEntity<TKey>
      // =========================================================
      .Where(t => ImplementsEntityInterface(t))

      // =========================================================
      // Exclude aggregates explicitly
      // =========================================================
      .Where(t => !IsAggregateRoot(t))

      .ToList();
  }

  private static bool IsAggregateRoot(Type type)
  {
    return type.GetInterfaces()
        .Any(i => i.IsGenericType &&
                  i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));
  }

  #endregion

  #region BEHAVIORS

  public static IServiceCollection AddBehaviors(this IServiceCollection services)
  {
    services.AddScoped(typeof(IPipeline<,>), typeof(PersistenceBehavior<,>));
    return services;
  }

  #endregion

  #region Unit of Work Registarion
  public static IServiceCollection AddUnitOfWork<TDbContext>(
      this IServiceCollection services)
      where TDbContext : DbContextBase
  {
    services.AddScoped<IUnitOfWork, EfUnitOfWork<TDbContext>>();
    return services;
  }

  #endregion
}