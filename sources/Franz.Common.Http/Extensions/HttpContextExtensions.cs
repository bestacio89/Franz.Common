namespace Microsoft.AspNetCore.Http;

public static class HttpContextExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static string? TryGetValue(this HttpContext? httpContext, string headerName, string? claimType = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    string? result = null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    if (httpContext != null)
    {
      if (claimType != null)
        result = httpContext.User?.Claims.SingleOrDefault(c => c.Type == claimType)?.Value;

      if (string.IsNullOrEmpty(result) &&
          httpContext.Request.Headers.ContainsKey(headerName))
      {
        result = httpContext.Request.Headers[headerName];
      }
    }

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IEnumerable<string>? TryGetValues(this HttpContext? httpContext, string headerName, string? claimType = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    IEnumerable<string>? results = null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    if (httpContext != null)
    {
      if (claimType != null)
        results = httpContext?.User?.Claims.Where(claim => claim.Type == claimType).Select(claim => claim.Value);

      if (results?.Any() != true &&
          httpContext!.Request.Headers.ContainsKey(headerName))
      {
        results = httpContext.Request.Headers[headerName];
      }
    }

    return results;
  }
}