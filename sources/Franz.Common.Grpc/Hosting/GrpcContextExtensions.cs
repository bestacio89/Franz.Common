using Grpc.Core;
using System.Linq;

namespace Franz.Common.Grpc.Hosting;

/// <summary>
/// Extensions for extracting metadata values (tenant, user, correlation, custom headers)
/// from the gRPC ServerCallContext.
/// </summary>
public static class GrpcContextExtensions
{
  /// <summary>
  /// Attempts to read a single value from Metadata (gRPC headers).
  /// </summary>
  public static string? TryGetHeader(
      this ServerCallContext? ctx,
      string headerName)
  {
    if (ctx is null)
      return null;

    var entry = ctx.RequestHeaders
        .FirstOrDefault(h => h.Key.Equals(headerName, System.StringComparison.OrdinalIgnoreCase));

    return entry?.Value;
  }

  /// <summary>
  /// Attempts to read multiple values from Metadata (gRPC headers).
  /// </summary>
  public static string[]? TryGetHeaders(
      this ServerCallContext? ctx,
      string headerName)
  {
    if (ctx is null)
      return null;

    var entries = ctx.RequestHeaders
        .Where(h => h.Key.Equals(headerName, System.StringComparison.OrdinalIgnoreCase))
        .Select(h => h.Value)
        .ToArray();

    return entries.Length > 0 ? entries : null;
  }

  /// <summary>
  /// Attempts to read metadata values from both request headers and trailers.
  /// </summary>
  public static string? TryGetHeaderOrTrailer(
      this ServerCallContext? ctx,
      string name)
  {
    if (ctx is null)
      return null;

    var header = ctx.TryGetHeader(name);
    if (!string.IsNullOrEmpty(header))
      return header;

    var trailer = ctx.ResponseTrailers
        ?.FirstOrDefault(t => t.Key.Equals(name, System.StringComparison.OrdinalIgnoreCase));

    return trailer?.Value;
  }
}
