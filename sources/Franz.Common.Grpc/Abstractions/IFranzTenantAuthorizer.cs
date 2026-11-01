using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Authorizes the authenticated caller for a tenant resolved from trusted server-side identity.
/// Implementations must not treat x-tenant-id or x-user-id metadata as proof of identity or membership.
/// </summary>
public interface IFranzTenantAuthorizer
{
    Task<bool> IsAuthorizedAsync(
        GrpcCallContext context,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
