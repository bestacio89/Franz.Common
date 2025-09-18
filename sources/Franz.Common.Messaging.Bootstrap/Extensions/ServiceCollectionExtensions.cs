using Franz.Common.Bootstrap.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Franz.Common.Messaging.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Messaging.Identity.Extensions;
using Franz.Common.Messaging.MultiTenancy.Extensions;
namespace Franz.Common.Messaging.Bootstrap.Extenstions;

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