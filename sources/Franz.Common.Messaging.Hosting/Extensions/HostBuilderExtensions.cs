using Franz.Common.Errors;
using Franz.Common.Logging.Extensions;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Hosting.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IHostBuilder UseMessaging<TStartup>(this IHostBuilder hostBuilder, Assembly? assembly = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
      where TStartup : class
  {
    if (assembly == null)
      assembly = Assembly.GetCallingAssembly();

    var result = hostBuilder
      .LoadAssemblyReferencedNotLoaded(assembly)
      .UseLog()
      .ConfigureServices((hostingContext, services) =>
      {
        services.AddHostedService<MessagingHostedService>();

        var startup = Create<TStartup>(hostingContext);
        CallConfigureServices(services, startup);
      });

    return result;
  }

  private static TStartup Create<TStartup>(HostBuilderContext hostingContext)
    where TStartup : class
  {
    var constructor = typeof(TStartup).GetConstructor(new Type[] { typeof(IHostEnvironment), typeof(IConfiguration) }) ?? throw new TechnicalException(string.Format(Resources.HostBuilderExtensionsNoConstructorFoundException, typeof(IHostEnvironment), typeof(IConfiguration)));
    var result = (TStartup)constructor.Invoke(new object[] { hostingContext.HostingEnvironment, hostingContext.Configuration });

    return result;
  }

  private static void CallConfigureServices<TStartup>(IServiceCollection services, TStartup startup)
    where TStartup : class
  {
    var configureServicesMethod = typeof(TStartup).GetMethod("ConfigureServices") ?? throw new TechnicalException(Resources.HostBuilderExtensionsNoMethodFoundException);
    configureServicesMethod!.Invoke(startup, new object[] { services });
  }
}
