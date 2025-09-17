using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Headers;
using Franz.Common.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Http.Headers.Extensions;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddHttpHeaderContext(this IServiceCollection services)
  {
    services
      .AddHttpContextAccessor()
      .AddNoDuplicateScoped<IHeaderContextAccessor, HeaderContextAccessor>();

    return services;
  }

  public static IServiceCollection AddHeaderRequiredCapability(this IServiceCollection services)
  {
    services.AddNoDuplicateTransient<HeaderRequiredActionConstraint>();

    return services;
  }
}
