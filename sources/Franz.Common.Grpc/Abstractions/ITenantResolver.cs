using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Resolves tenant information for an incoming gRPC call.
/// This mirrors the Franz tenant resolution architecture in the HTTP stack.
/// </summary>
public interface IFranzTenantResolver
{
  /// <summary>
  /// Attempts to resolve a tenant identifier from the request context.
  /// Should return null if no tenant can be resolved.
  /// </summary>
  Task<string?> ResolveTenantAsync(
      GrpcCallContext context,
      CancellationToken cancellationToken = default);
}
