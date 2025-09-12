// Middleware/TenantResolutionMiddleware.cs
using System.Linq;
using System.Threading.Tasks;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Middleware
{
  public class TenantResolutionMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ITenantResolutionPipeline _pipeline;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IDomainContextAccessor _domainContextAccessor;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ITenantResolutionPipeline pipeline,
        ITenantContextAccessor tenantContextAccessor,
        IDomainContextAccessor domainContextAccessor)
    {
      _next = next;
      _pipeline = pipeline;
      _tenantContextAccessor = tenantContextAccessor;
      _domainContextAccessor = domainContextAccessor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var result = await _pipeline.ResolveAsync(context);

      if (result?.Tenant != null)
      {
        _tenantContextAccessor.SetCurrentTenantId(result.Tenant.Id);

        var matchedDomain = result.Tenant.Domains.FirstOrDefault(d =>
            string.Equals(d.Host, context.Request.Host.Host,
                System.StringComparison.OrdinalIgnoreCase));

        if (matchedDomain != null)
        {
          _domainContextAccessor.SetCurrentDomainId(result.Tenant.Id);
        }
      }

      await _next(context);
    }
  }
}
