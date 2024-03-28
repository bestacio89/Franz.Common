using Franz.Common.DependencyInjection;
using Scrutor;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddDependencies(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {

    services = services
      .AddScopedDependencies(assemblyPredicate)
      .AddSingletonDependencies(assemblyPredicate);


    return services;
  }

  public static IServiceCollection AddScopedDependencies(this IServiceCollection services, Func<Assembly, bool> assemblyPredicate = null)

  {
    services = services.AddImplementedInterfaceWithoutMarkerScoped<IScopedDependency>(assemblyPredicate);

    return services;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddSingletonDependencies(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    services = services.AddImplementedInterfaceWithoutMarkerSingleton<ISingletonDependency>(assemblyPredicate);

    return services;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddSelfScoped<TInterface>(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    services = services.Scan(scan => scan
      .FromCompanyApplicationDependenciesWithPredicate(assemblyPredicate)
      .AddClasses(classes => classes.AssignableTo<TInterface>())
        .UsingRegistrationStrategy(RegistrationStrategy.Skip)
        .AsSelf()
        .WithScopedLifetime());

    return services;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddMatchingInterfaceScoped<TInterface>(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    services = services.Scan(scan => scan
      .FromCompanyApplicationDependenciesWithPredicate(assemblyPredicate)
      .AddClasses(classes => classes.AssignableTo<TInterface>())
      .UsingRegistrationStrategy(new RegistrationStrategySkipExistingPair())
      .AsMatchingInterface()
      .WithScopedLifetime());

    return services;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddImplementedInterfaceScoped<TInterface>(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    services = services.Scan(scan => scan
      .FromCompanyApplicationDependenciesWithPredicate(assemblyPredicate)
      .AddClasses(classes => classes.AssignableTo<TInterface>())
      .UsingRegistrationStrategy(new RegistrationStrategySkipExistingPair())
      .AsImplementedInterfaces()
      .WithScopedLifetime());

    return services;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddImplementedInterfaceTransient<TInterface>(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    services = services.Scan(scan => scan
      .FromCompanyApplicationDependenciesWithPredicate(assemblyPredicate)
      .AddClasses(classes => classes.AssignableTo<TInterface>())
      .UsingRegistrationStrategy(new RegistrationStrategySkipExistingPair())
      .AsImplementedInterfaces()
      .WithTransientLifetime());

    return services;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddImplementedInterfaceWithoutMarkerScoped<TInterface>(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var markerType = typeof(TInterface);
    services = services.Scan(scan => scan
      .FromCompanyApplicationDependenciesWithPredicate(assemblyPredicate)
      .AddClasses(classes => classes.AssignableTo<TInterface>())
      .UsingRegistrationStrategy(new RegistrationStrategySkipExistingPair())
      .As(type => type.GetInterfaces().Where(i => !i.Equals(markerType)))
      .WithScopedLifetime());

    return services;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddImplementedInterfaceWithoutMarkerSingleton<TInterface>(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var markerType = typeof(TInterface);
    services = services.Scan(scan => scan
      .FromCompanyApplicationDependenciesWithPredicate(assemblyPredicate)
      .AddClasses(classes => classes.AssignableTo<TInterface>())
      .UsingRegistrationStrategy(new RegistrationStrategySkipExistingPair())
      .As(type => type.GetInterfaces().Where(i => !i.Equals(markerType)))
      .WithSingletonLifetime());

    return services;
  }


#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddImplementedInterfaceSingleton<TInterface>(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null, RegistrationStrategy? registrationStrategy = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

  {
    registrationStrategy ??= new RegistrationStrategySkipExistingPair();

    services = services.Scan(scan => scan
      .FromCompanyApplicationDependenciesWithPredicate(assemblyPredicate)
      .AddClasses(classes => classes.AssignableTo<TInterface>())
      .UsingRegistrationStrategy(registrationStrategy)
      .AsImplementedInterfaces()
      .WithSingletonLifetime());

    return services;
  }


#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddInheritedClassSingleton<TType>(this IServiceCollection services, Func<Assembly, bool>? assemblyPredicate = null, RegistrationStrategy? registrationStrategy = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

  {
    registrationStrategy ??= new RegistrationStrategySkipExistingPair();

    services = services.Scan(scan => scan
      .FromCompanyApplicationDependenciesWithPredicate(assemblyPredicate)
      .AddClasses(classes => classes.AssignableTo<TType>())
      .UsingRegistrationStrategy(registrationStrategy)
      .As<TType>()
      .WithSingletonLifetime());

    return services;
  }

  public static IServiceCollection AddNoDuplicateService(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime serviceLifetime)
  {
    var serviceDescriptrToAdd = new ServiceDescriptor(serviceType, implementationType, serviceLifetime);

    new RegistrationStrategySkipExistingPair().Apply(services, serviceDescriptrToAdd);

    return services;
  }

  public static IServiceCollection AddNoDuplicateService<TServiceType, TImplementationType>(this IServiceCollection services, ServiceLifetime serviceLifetime)
    where TServiceType : class
    where TImplementationType : class, TServiceType
  {
    services = services.AddNoDuplicateService(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    return services;
  }

  private static IServiceCollection AddNoDuplicateService<TImplementationType>(this IServiceCollection services, ServiceLifetime serviceLifetime, Func<IServiceProvider, TImplementationType> factoryInstanceService)
    where TImplementationType : class
  {
    var serviceDescriptrToAdd = new ServiceDescriptor(typeof(TImplementationType), factoryInstanceService, serviceLifetime);

    new RegistrationStrategySkipExistingPair().Apply(services, serviceDescriptrToAdd);

    return services;
  }

  public static IServiceCollection AddNoDuplicateSingleton<TServiceType, TImplementationType>(this IServiceCollection services)
    where TServiceType : class
    where TImplementationType : class, TServiceType
  {
    services = services.AddNoDuplicateService<TServiceType, TImplementationType>(ServiceLifetime.Singleton);

    return services;
  }

  public static IServiceCollection AddNoDuplicateSingleton(this IServiceCollection services, Type serviceType, Type implementationType)
  {
    services = services.AddNoDuplicateService(serviceType, implementationType, ServiceLifetime.Singleton);

    return services;
  }

  public static IServiceCollection AddNoDuplicateSingleton<TImplementationType>(this IServiceCollection services)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService<TImplementationType, TImplementationType>(ServiceLifetime.Singleton);

    return services;
  }

  public static IServiceCollection AddNoDuplicateSingleton<TImplementationType>(this IServiceCollection services, TImplementationType instanceImplementation)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService(ServiceLifetime.Singleton, (serviceProvider) => instanceImplementation);

    return services;
  }

  public static IServiceCollection AddNoDuplicateSingleton<TImplementationType>(this IServiceCollection services, Func<IServiceProvider, TImplementationType> factoryInstanceImplementation)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService(ServiceLifetime.Singleton, factoryInstanceImplementation);

    return services;
  }

  public static IServiceCollection AddNoDuplicateTransient<TServiceType, TImplementationType>(this IServiceCollection services)
    where TServiceType : class
    where TImplementationType : class, TServiceType
  {
    services = services.AddNoDuplicateService<TServiceType, TImplementationType>(ServiceLifetime.Transient);

    return services;
  }

  public static IServiceCollection AddNoDuplicateTransient(this IServiceCollection services, Type serviceType, Type implementationType)
  {
    services = services.AddNoDuplicateService(serviceType, implementationType, ServiceLifetime.Transient);

    return services;
  }

  public static IServiceCollection AddNoDuplicateTransient<TImplementationType>(this IServiceCollection services)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService<TImplementationType, TImplementationType>(ServiceLifetime.Transient);

    return services;
  }

  public static IServiceCollection AddNoDuplicateTransient<TImplementationType>(this IServiceCollection services, TImplementationType instanceImplementation)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService(ServiceLifetime.Transient, (serviceProvder) => instanceImplementation);

    return services;
  }

  public static IServiceCollection AddNoDuplicateTransient<TImplementationType>(this IServiceCollection services, Func<IServiceProvider, TImplementationType> factoryInstanceImplementation)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService(ServiceLifetime.Transient, factoryInstanceImplementation);

    return services;
  }

  public static IServiceCollection AddNoDuplicateScoped<TServiceType, TImplementationType>(this IServiceCollection services)
    where TServiceType : class
    where TImplementationType : class, TServiceType
  {
    services = services.AddNoDuplicateService<TServiceType, TImplementationType>(ServiceLifetime.Scoped);

    return services;
  }

  public static IServiceCollection AddNoDuplicateScoped(this IServiceCollection services, Type serviceType, Type implementationType)
  {
    services = services.AddNoDuplicateService(serviceType, implementationType, ServiceLifetime.Scoped);

    return services;
  }

  public static IServiceCollection AddNoDuplicateScoped<TImplementationType>(this IServiceCollection services)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService<TImplementationType, TImplementationType>(ServiceLifetime.Scoped);

    return services;
  }

  public static IServiceCollection AddNoDuplicateScoped<TImplementationType>(this IServiceCollection services, TImplementationType instanceImplementation)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService(ServiceLifetime.Scoped, (serviceProvider) => instanceImplementation);

    return services;
  }

  public static IServiceCollection AddNoDuplicateScoped<TImplementationType>(this IServiceCollection services, Func<IServiceProvider, TImplementationType> factoryInstanceImplementation)
    where TImplementationType : class
  {
    services = services.AddNoDuplicateService(ServiceLifetime.Scoped, factoryInstanceImplementation);

    return services;
  }
}
