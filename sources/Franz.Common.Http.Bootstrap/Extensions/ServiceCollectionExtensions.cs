#nullable enable
using Franz.Common.Bootstrap.Extensions;
using Franz.Common.Http.Authentication.Extensions;
using Franz.Common.Http.MultiTenancy.Extensions;
using Franz.Common.Http.Headers.Extensions;
using Franz.Common.Http.Identity.Extensions;
using Franz.Common.Http.Documentation.Extensions;
using Franz.Common.Serialization.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Franz.Common.Http.Bootstrap.Extensions;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Adds the Franz common HTTP architecture: controllers, auth, routing, 
  /// multi-tenancy, serialization, errors, and health checks.
  /// </summary>
  public static IServiceCollection AddHttpArchitecture(
      this IServiceCollection services,
      IHostEnvironment hostEnvironment,
      IConfiguration configuration,
      Assembly? assembly = null)
  {
    assembly ??= Assembly.GetCallingAssembly();

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
        

    // Note: Refit support is now provided in Franz.Common.Http.Refit
    // via AddFranzRefit<TClient>. Consumers must opt-in explicitly.

    return services;
  }
}
