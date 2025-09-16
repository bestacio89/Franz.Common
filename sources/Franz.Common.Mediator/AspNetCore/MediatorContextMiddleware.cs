using Franz.Common.Mediator.Context;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.AspNetCore
{
  public sealed class MediatorContextMiddleware
  {
    private readonly RequestDelegate _next;

    public MediatorContextMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
      try
      {
        // Reset context for this request
        MediatorContext.Reset();

        // Always generate a new correlation ID if missing
        MediatorContext.Current.CorrelationId =
            httpContext.TraceIdentifier ?? Guid.NewGuid().ToString();

        // Capture authenticated user ID (if any)
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
          MediatorContext.Current.UserId =
              httpContext.User.Identity.Name;
        }

        // Capture tenant ID if passed in headers (customize as needed)
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
        {
          MediatorContext.Current.TenantId = tenantId.ToString();
        }

        // Capture culture (from request localization or headers)
        MediatorContext.Current.Culture =
            CultureInfo.CurrentCulture;

        // Continue pipeline
        await _next(httpContext);
      }
      finally
      {
        // Ensure context does not leak across requests
        MediatorContext.Reset();
      }
    }
  }
}
