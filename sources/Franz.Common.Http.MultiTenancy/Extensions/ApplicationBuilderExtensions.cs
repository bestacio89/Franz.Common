// Extensions/ApplicationBuilderExtensions.cs
using Franz.Common.Http.MultiTenancy.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Franz.Common.Http.MultiTenancy.Extensions
{
  public static class ApplicationBuilderExtensions
  {
    public static IApplicationBuilder UseFranzMultiTenancy(this IApplicationBuilder app)
    {
      return app.UseMiddleware<TenantResolutionMiddleware>();
    }
  }
}
