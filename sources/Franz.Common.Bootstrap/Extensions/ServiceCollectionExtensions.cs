using Franz.Common.Mediator.Extensions;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Reflection.Extensions;

public static class ServiceCollectionExtensions
{
#pragma warning disable CS8632
  /// <summary>
  /// Registers the common architecture services, including mediator, pipelines, dependencies, and AutoMapper.
  /// </summary>
  /// <param name="services">The IServiceCollection instance.</param>
  /// <param name="configuration">The configuration object.</param>
  /// <param name="assemblies">Optional assemblies to scan for handlers.</param>
  /// <returns>The IServiceCollection instance for chaining.</returns>
  public static IServiceCollection AddCommonArchitecture(
      this IServiceCollection services,
      IConfiguration configuration,
      params Assembly[] assemblies)
#pragma warning restore CS8632
  {
    // If no assemblies provided, default to calling assembly
    if (assemblies == null || assemblies.Length == 0)
    {
      assemblies = new[] { Assembly.GetCallingAssembly() };
    }

    // Register reflection-based services or automatic scanning for the calling assemblies
    foreach (var assembly in assemblies)
    {
      services.AddCallingAssembly(assembly);
    }

    // Register your Franz mediator (commands, queries, pipelines)
    foreach (var assembly in assemblies)
    {
      services.AddFranzMediator(new[] { assembly }); // registers dispatcher, handlers, Franz pipelines
    }

    // Register any optional dependencies
    services.AddDependencies();

    // Register AutoMapper
    services.AddAutoMapper(assemblies);

    return services;
  }
}
