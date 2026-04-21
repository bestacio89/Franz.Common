using Franz.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Hosting.Extensions;

public static class ServiceProviderExtensions
{
  public static async Task InitializeAsync(this IServiceProvider serviceProvider)
  {
    using var scope = serviceProvider.CreateScope();

    var hostingInitializers = scope.ServiceProvider
      .GetServices<IHostingInitializer>()
      .OrderBy(x => x.Order)
      .ToList();

    foreach (var initializer in hostingInitializers)
    {
      await initializer.InitializeAsync();
    }
  }
}