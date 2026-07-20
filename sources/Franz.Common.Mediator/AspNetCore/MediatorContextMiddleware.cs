using Franz.Common.Mediator.Context;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.AspNetCore;

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
      MediatorContext.Reset();

      var context = MediatorExecutionContext.Empty;

      // Correlation ID
      var rawId = httpContext.TraceIdentifier;
      var correlationId = Guid.TryParse(rawId, out var parsed)
          ? parsed
          : Guid.CreateVersion7();

      context = context.WithCorrelationId(correlationId);

      // User
      if (httpContext.User?.Identity?.IsAuthenticated == true)
      {
        context = context.WithUser(httpContext.User.Identity.Name);
      }

      // Tenant
      if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
      {
        context = context.WithTenant(tenantId.ToString());
      }

      // Culture
      context = context.WithCulture(CultureInfo.CurrentCulture);

      MediatorContext.Set(context);

      await _next(httpContext);
    }
    finally
    {
      MediatorContext.Reset();
    }
  }
}