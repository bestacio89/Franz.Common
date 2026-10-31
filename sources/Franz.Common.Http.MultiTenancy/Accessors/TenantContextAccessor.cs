using Franz.Common.MultiTenancy;
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
        if (items is not null) items[HttpTenantContextKeys.TenantId] = tenantId;
    }
}
