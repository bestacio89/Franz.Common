using Franz.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace System;

public static class ServiceProviderExtensions
{
  public static void Initialize(this IServiceProvider serviceProvider)
  {
    using var scope = serviceProvider.CreateScope();

    var hostingInitializers = List(scope);

    hostingInitializers = hostingInitializers.OrderBy(hostingInitializer => hostingInitializer.Order);

    foreach (var hostingInitializer in hostingInitializers)
      hostingInitializer.Initialize();
  }

  private static IEnumerable<IHostingInitializer> List(IServiceScope scope)
  {
    var scopeServiceProvider = scope.ServiceProvider;

    var results = scopeServiceProvider.GetServices<IHostingInitializer>();

    return results;
  }
}
