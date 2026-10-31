using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.MultiTenancy;

/// <summary>Provides validated tenant identity isolated to the current execution scope.</summary>
public interface ITenantExecutionContextAccessor
{
    TenantExecutionContext? Current { get; }
    void Initialize(TenantExecutionContext context);
    void Clear();
}
