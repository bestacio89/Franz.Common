// File: Models/TenantInfo.cs
using System;
using System.Collections.Generic;
#nullable enable
namespace Franz.Common.MultiTenancy.Models
{
  /// <summary>
  /// Tenant metadata. Keep this lightweight in the core package.
  /// </summary>
  public sealed class TenantInfo
  {
    public TenantInfo(Guid id, string name)
    {
      Id = id;
      Name = name ?? string.Empty;
      Domains = new List<TenantDomain>();
      Properties = new Dictionary<string, string>();
    }

    public Guid Id { get; init; }
    public string Name { get; init; }
    public IList<TenantDomain> Domains { get; init; }
    public IDictionary<string, string> Properties { get; init; }

    /// <summary>
    /// Optional connection string or other sensitive data should be placed in a secure store,
    /// and referenced here by a key (not the actual secret) in production systems.
    /// </summary>
    public string? ConnectionStringKey { get; set; }
  }
}
