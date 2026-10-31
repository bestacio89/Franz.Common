using Franz.Common.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Accessors;

/// <summary>Reads only server-established domain state, never caller-supplied headers.</summary>
public sealed class DomainContextAccessor(IHttpContextAccessor httpContextAccessor) : IDomainContextAccessor
{
    public Guid? GetCurrentDomainId()
    {
        var items = httpContextAccessor.HttpContext?.Items;
        return items is not null && items.TryGetValue(HttpTenantContextKeys.DomainId, out var value) && value is Guid id
            ? id
            : null;
    }

    public void SetCurrentDomainId(Guid domainId)
    {
        var items = httpContextAccessor.HttpContext?.Items;
        if (items is not null) items[HttpTenantContextKeys.DomainId] = domainId;
    }
}
