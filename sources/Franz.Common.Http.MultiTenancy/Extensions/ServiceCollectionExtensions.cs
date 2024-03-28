using Franz.Common.Http.MultiTenancy;
using Franz.Common.Http.MultiTenancy.Documentation;
using Franz.Common.MultiTenancy;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddHttpMultitenancyContext(this IServiceCollection services)
  {
    services
      .AddHttpContextAccessor()
      .AddNoDuplicateScoped<ITenantContextAccessor, TenantContextAccessor>()
      .AddNoDuplicateScoped<IDomainContextAccessor, DomainContextAccessor>()
      .AddSwaggerGen(options =>
      {
        options.OperationFilter<AddRequiredHeaderParameter>();
      });

    return services;
  }
}
