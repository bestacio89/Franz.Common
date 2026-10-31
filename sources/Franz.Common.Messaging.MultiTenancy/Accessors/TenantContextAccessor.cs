#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy.Accessors;

/// <summary>Reads validated tenant identity from execution context and writes metadata only for outbound propagation.</summary>
public sealed class TenantContextAccessor(
    IMessageContextAccessor messageContextAccessor,
    ITenantExecutionContextAccessor executionContextAccessor) : ITenantContextAccessor
{
    public Guid? GetCurrentId() => GetCurrentTenantId();
    public void SetCurrentId(Guid tenantId) => SetCurrentTenantId(tenantId);

    public Guid? GetCurrentTenantId() => executionContextAccessor.Current?.TenantId;

    public void SetCurrentTenantId(Guid tenantId)
    {
        var headers = messageContextAccessor.Current?.Message.Headers;
        if (headers is not null)
            headers[HeaderConstants.TenantId] = [tenantId.ToString()];
    }
}
