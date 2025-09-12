using Franz.Common.Messaging.MultiTenancy.Accessors;
using Franz.Common.MultiTenancy;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMessagingMultitenancyContext(this IServiceCollection services)
  {
    services
      .AddNoDuplicateScoped<ITenantContextAccessor, TenantContextAccessor>()
      .AddNoDuplicateScoped<IDomainContextAccessor, DomainContextAccessor>();

    return services;
  }
}
