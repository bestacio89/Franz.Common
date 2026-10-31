#nullable enable
namespace Franz.Common.MultiTenancy.Models;

/// <summary>Lightweight tenant registry metadata. Secrets belong in a dedicated secret store.</summary>
public sealed class TenantInfo
{
    public TenantInfo(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Tenant identifier cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tenant name is required.", nameof(name));
        Id = id;
        Name = name.Trim();
        Domains = new List<TenantDomain>();
        Properties = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public Guid Id { get; init; }
    public string Name { get; init; }
    public IList<TenantDomain> Domains { get; init; }
    public IDictionary<string, string> Properties { get; init; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    /// <summary>Reference to a secret-store entry; never store a connection string here.</summary>
    public string? ConnectionStringKey { get; set; }
}
