using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
  public static class ServiceCollectionExtensions
  {
    /// <summary>
    /// Registers AutoMapper with all profiles found in the calling assembly.
    /// </summary>
    public static IServiceCollection AddAutoMapper(this IServiceCollection services, params Assembly[] assemblies)
    {
      if (assemblies == null || assemblies.Length == 0)
      {
        assemblies = new[] { Assembly.GetCallingAssembly() };
      }

      // Register AutoMapper
      services.AddAutoMapper(assemblies);

      return services;
    }
  }
}
