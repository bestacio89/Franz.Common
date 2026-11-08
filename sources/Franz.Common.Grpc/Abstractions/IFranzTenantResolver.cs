using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>Resolves tenant information for an incoming gRPC call.</summary>
public interface IFranzTenantResolver
{
    /// <summary>Attempts to resolve a tenant identifier; returns null when none is available.</summary>
    Task<string?> ResolveTenantAsync(
        GrpcCallContext context,
        CancellationToken cancellationToken = default);
}
