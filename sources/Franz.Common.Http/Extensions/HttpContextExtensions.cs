using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Http.Extensions;

public static class HttpContextExtensions
{
  /// <summary>
  /// Attempts to get a value from a claim or header.
  /// </summary>
  public static string? TryGetValue(
      this HttpContext? httpContext,
      string headerName,
      string? claimType = null)
  {
    if (httpContext is null)
      return null;

    // Try claims first
    if (!string.IsNullOrWhiteSpace(claimType))
    {
      var claimValue = httpContext.User?
          .Claims
          .FirstOrDefault(c => c.Type == claimType)
          ?.Value;

      if (!string.IsNullOrEmpty(claimValue))
        return claimValue;
    }

    // Try headers
    if (httpContext.Request.Headers.TryGetValue(headerName, out var values))
    {
      return values.FirstOrDefault();
    }

    return null;
  }

  /// <summary>
  /// Attempts to get multiple values from claims or header.
  /// </summary>
  public static IEnumerable<string>? TryGetValues(
      this HttpContext? httpContext,
      string headerName,
      string? claimType = null)
  {
    if (httpContext is null)
      return null;

    IEnumerable<string>? claimResults = null;

    // Try claims first
    if (!string.IsNullOrWhiteSpace(claimType))
    {
      claimResults = httpContext.User?
          .Claims
          .Where(c => c.Type == claimType)
          .Select(c => c.Value);

      if (claimResults is not null && claimResults.Any())
        return claimResults;
    }

    // Try headers
    if (httpContext.Request.Headers.TryGetValue(headerName, out var headerValues))
    {
      return headerValues;
    }

    return null;
  }
}
