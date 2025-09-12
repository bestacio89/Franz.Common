// Extensions/MultiTenancyServiceCollectionExtensions.cs
using Franz.Common.Http.MultiTenancy.Accessors;
using Franz.Common.Http.MultiTenancy.Middleware;
using Franz.Common.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Http.MultiTenancy.Extensions
{
  public static class MultiTenancyServiceCollectionExtensions
  {
    public static IServiceCollection AddFranzMultiTenancy(this IServiceCollection services)
    {
      services.AddHttpContextAccessor();
      services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
      services.AddScoped<IDomainContextAccessor, DomainContextAccessor>();
      services.AddScoped<ITenantResolutionPipeline, DefaultTenantResolutionPipeline>();
      services.AddScoped<IDomainResolutionPipeline, DefaultDomainResolutionPipeline>();

      return services;
    }
  }
}
