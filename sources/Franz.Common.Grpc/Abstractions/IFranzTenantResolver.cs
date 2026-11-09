using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Resolves tenant information for an incoming gRPC call.
/// </summary>
/// <remarks>
/// The context may contain a caller-supplied tenant identifier as a selection
/// hint. Implementations must resolve it against trusted server-side data and
/// must never treat it as proof of tenant membership. Membership authorization
/// is performed separately after resolution.
/// </remarks>
public interface IFranzTenantResolver
{
    /// <summary>Attempts to resolve a tenant identifier; returns null when none is available.</summary>
    Task<string?> ResolveTenantAsync(
        GrpcCallContext context,
        CancellationToken cancellationToken = default);
}
