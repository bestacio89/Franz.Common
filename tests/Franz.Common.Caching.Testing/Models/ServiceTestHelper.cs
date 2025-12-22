using Microsoft.Extensions.DependencyInjection;
using System;

namespace Franz.Common.Caching.Testing.Models;

internal static class ServiceTestHelper
{
  public static ServiceProvider Build(Action<IServiceCollection> configure)
  {
    var services = new ServiceCollection();
    configure(services);
    return services.BuildServiceProvider();
  }
}
