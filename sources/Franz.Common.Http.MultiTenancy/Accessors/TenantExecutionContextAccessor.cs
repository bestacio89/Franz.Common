using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.Http.MultiTenancy.Accessors;

/// <summary>Stores an immutable validated context in the current HttpContext, isolating concurrent requests.</summary>
public sealed class TenantExecutionContextAccessor(IHttpContextAccessor httpContextAccessor) : ITenantExecutionContextAccessor
{
    public TenantExecutionContext? Current => httpContextAccessor.HttpContext?.Items.TryGetValue(HttpTenantContextKeys.ExecutionContext, out var value) == true
        ? value as TenantExecutionContext
        : null;

    public void Initialize(TenantExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var items = httpContextAccessor.HttpContext?.Items ?? throw new InvalidOperationException("Tenant context can only be initialized within an HTTP request.");
        if (items.ContainsKey(HttpTenantContextKeys.ExecutionContext))
            throw new InvalidOperationException("Tenant execution context has already been initialized for this request.");
        items[HttpTenantContextKeys.ExecutionContext] = context;
        items[HttpTenantContextKeys.TenantId] = context.TenantId;
        if (context.DomainId is Guid domainId) items[HttpTenantContextKeys.DomainId] = domainId;
    }

    public void Clear()
    {
        var items = httpContextAccessor.HttpContext?.Items;
        if (items is null) return;
        items.Remove(HttpTenantContextKeys.ExecutionContext);
        items.Remove(HttpTenantContextKeys.TenantId);
        items.Remove(HttpTenantContextKeys.DomainId);
    }
}
