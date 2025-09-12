// File: Models/TenantDomain.cs
using System;

namespace Franz.Common.MultiTenancy.Models
{
  /// <summary>
  /// Represents a domain (hostname) that belongs to a tenant.
  /// Example: tenant1.myapp.com or www.example-tenant.com
  /// </summary>
  public sealed record TenantDomain
  {
    public TenantDomain(string host, bool isPrimary = false)
    {
      Host = host?.Trim()?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(host));
      IsPrimary = isPrimary;
    }

    /// <summary>Canonical host (lowercase, no scheme, no path)</summary>
    public string Host { get; init; }

    /// <summary>True when this domain is the canonical / primary domain for the tenant.</summary>
    public bool IsPrimary { get; init; }
  }
}
