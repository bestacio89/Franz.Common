using Franz.Common.Business.Domain;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.AzureCosmosDB.Repositories;

namespace Franz.Common.AzureCosmosDB.Tests;

public class CosmosEntity : Entity<Guid>
{
    public CosmosEntity() { }
    protected CosmosEntity(Guid id) : base(id) { }
    public string Label { get; set; } = string.Empty;
}

/// <summary>Tenant-owned document used to verify Cosmos partition and query isolation.</summary>
public sealed class TenantCosmosEntity : Entity<Guid>, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Label { get; set; } = string.Empty;
}


/// <summary>Test-only scoped accessor; tests initialize it with a validated tenant context.</summary>
public sealed class CosmosTestTenantExecutionContextAccessor : ITenantExecutionContextAccessor
{
    public TenantExecutionContext? Current { get; private set; }

    public void Initialize(TenantExecutionContext context)
    {
        if (Current is not null)
            throw new TenantIsolationException("Tenant context is already initialized.");
        Current = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Clear() => Current = null;
}


/// <summary>Native Cosmos SDK document used to test partition-key-bound repository operations.</summary>
public sealed class CosmosSdkTenantDocument : ICosmosTenantDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string Label { get; set; } = string.Empty;
}
