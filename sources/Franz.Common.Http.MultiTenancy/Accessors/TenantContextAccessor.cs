using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Accessors;

/// <summary>Reads only server-established tenant state; request headers are never authoritative here.</summary>
public sealed class TenantContextAccessor(IHttpContextAccessor httpContextAccessor) : ITenantContextAccessor
{
    public Guid? GetCurrentTenantId()
    {
        var items = httpContextAccessor.HttpContext?.Items;
        return items is not null && items.TryGetValue(HttpTenantContextKeys.TenantId, out var value) && value is Guid id
            ? id
            : null;
    }

    public void SetCurrentTenantId(Guid tenantId)
    {
        var items = httpContextAccessor.HttpContext?.Items;
        if (items is null)
            return;

        if (items.TryGetValue(HttpTenantContextKeys.ExecutionContext, out var value) &&
            value is TenantExecutionContext executionContext)
        {
            if (executionContext.TenantId != tenantId)
                throw new InvalidOperationException("The validated tenant execution context cannot be replaced during an HTTP request.");
            return;
        }

        items[HttpTenantContextKeys.TenantId] = tenantId;
    }
}
