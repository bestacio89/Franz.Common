using Franz.Common.Identity;
using Franz.Common.Messaging.Identity;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMessagingIdentityContext(this IServiceCollection services)
  {
    services
      .AddNoDuplicateScoped<IIdentityContextAccessor, IdentityContextAccessor>();

    return services;
  }
}
