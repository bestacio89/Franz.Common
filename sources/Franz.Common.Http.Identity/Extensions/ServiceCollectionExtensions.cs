using Franz.Common.Http.Identity;
using Franz.Common.Identity;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{

  public static IServiceCollection AddHttpIdentityContext(this IServiceCollection services)
  {
    services
      .AddHttpContextAccessor()
      .AddNoDuplicateScoped<IIdentityContextAccessor, IdentityContextAccessor>();

    return services;
  }
}
