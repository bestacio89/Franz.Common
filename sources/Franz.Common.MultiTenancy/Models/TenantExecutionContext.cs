namespace Franz.Common.MultiTenancy.Models;

/// <summary>
/// Immutable, validated tenant identity for one execution boundary.
/// A context is created only after tenant resolution and validation succeed.
/// </summary>
public sealed record TenantExecutionContext
{
    public TenantExecutionContext(TenantInfo tenant, Guid? domainId, TenantResolutionSource source, DateTimeOffset resolvedAt)
    {
        Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        if (tenant.Id == Guid.Empty)
            throw new ArgumentException("A tenant must have a non-empty identifier.", nameof(tenant));
        if (tenant.Status != TenantStatus.Active)
            throw new ArgumentException("Only active tenants can enter an execution context.", nameof(tenant));
        DomainId = domainId;
        Source = source;
        ResolvedAt = resolvedAt;
    }

    public TenantInfo Tenant { get; }
    public Guid TenantId => Tenant.Id;
    public Guid? DomainId { get; }
    public TenantResolutionSource Source { get; }
    public DateTimeOffset ResolvedAt { get; }
}
