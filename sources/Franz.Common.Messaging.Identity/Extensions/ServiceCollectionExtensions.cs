using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Identity;
using Franz.Common.Messaging.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Identity.Extensions;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMessagingIdentityContext(this IServiceCollection services)
  {
    services
      .AddNoDuplicateScoped<IIdentityContextAccessor, IdentityContextAccessor>();

    return services;
  }
}
