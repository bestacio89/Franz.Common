using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Messaging.MultiTenancy.Accessors;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.MultiTenancy.Extensions;
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
