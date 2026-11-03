using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.MultiTenancy;

/// <summary>
/// Holds the validated tenant context for one dependency-injection execution scope.
/// HTTP requests and message deliveries each receive an independent scoped instance.
/// </summary>
public sealed class ScopedTenantExecutionContextAccessor : ITenantExecutionContextAccessor
{
    public TenantExecutionContext? Current { get; private set; }

    public void Initialize(TenantExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Current is not null)
            throw new InvalidOperationException(
                "Tenant execution context has already been initialized for this execution scope.");

        Current = context;
    }

    public void Clear() => Current = null;
}
