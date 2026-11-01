using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.Messaging.MultiTenancy.Accessors;

public sealed class MessagingTenantExecutionContextAccessor : ITenantExecutionContextAccessor
{
    public TenantExecutionContext? Current { get; private set; }

    public void Initialize(TenantExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Current is not null)
            throw new InvalidOperationException("Tenant execution context has already been initialized for this message scope.");
        Current = context;
    }

    public void Clear() => Current = null;
}
