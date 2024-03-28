using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddMessagingArchitecture(this IServiceCollection services, IHostEnvironment hostEnvironment, IConfiguration configuration, Assembly? assembly = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    if (assembly == null)
      assembly = Assembly.GetCallingAssembly();

    services
      .AddCommonArchitecture(configuration, assembly)
      .AddMessagingIdentityContext()
      .AddMessagingMultitenancyContext()
      .AddMessagingHeaderContext();

    return services;
  }
}