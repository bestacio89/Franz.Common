using Franz.Common.Bootstrap.Extensions;
using Franz.Common.Http;
using Franz.Common.Http.Authentication.Extensions;
using Franz.Common.Http.MultiTenancy.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Franz.Common.Http.Headers.Extensions;
using Franz.Common.Http.Identity.Extensions;
using Franz.Common.Serialization.Extensions;
using Franz.Common.Http.Documentation.Extensions;

namespace Franz.Common.Http.Bootstrap.Extensions;
public static class ServiceCollectionExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddHttpArchitecture(this IServiceCollection services, IHostEnvironment hostEnvironment, IConfiguration configuration, Assembly? assembly = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    if (assembly == null)
      assembly = Assembly.GetCallingAssembly();

    services
      .AddCommonArchitecture(configuration, assembly)
      .AddHttpControllers()
      .AddFranzAuthentication()
      .AddFrenchRouting()
      .AddDefaultCors(configuration)
      .AddForwardedHeaders()
      .AddHttpHeaderContext()
      .AddHeaderRequiredCapability()
      .AddHttpIdentityContext()
      .AddFranzMultiTenancy()
      .AddSerializers()
      .AddHttpSerialization()
      .AddDocumentation()
      .AddHttpErrors()
      .AddHealthChecks();

    return services;
  }
}
