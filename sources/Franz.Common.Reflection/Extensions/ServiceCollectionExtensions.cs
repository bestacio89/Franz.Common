using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Franz.Common.Reflection.Extensions;

public static class ServiceCollectionExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddCallingAssembly(this IServiceCollection services, Assembly? assembly = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    if (assembly == null)
      assembly = Assembly.GetCallingAssembly();

    var assemblyAccessorWrapper = new AssemblyAccessorWrapper();

    services = services.AddSingleton<IAssemblyAccessor>(assemblyAccessorWrapper);

    return services;
  }
}
