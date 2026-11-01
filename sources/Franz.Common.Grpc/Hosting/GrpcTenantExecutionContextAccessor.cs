using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.Grpc.Hosting;

/// <summary>
/// Scoped tenant execution context for gRPC-only hosts.
/// HTTP hosts can register their HttpContext-backed implementation instead.
/// </summary>
public sealed class GrpcTenantExecutionContextAccessor : ITenantExecutionContextAccessor
{
    public TenantExecutionContext? Current { get; private set; }

    public void Initialize(TenantExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Current is not null)
            throw new TenantIsolationException("A gRPC execution scope cannot replace an existing tenant context.");

        Current = context;
    }

    public void Clear() => Current = null;
}
