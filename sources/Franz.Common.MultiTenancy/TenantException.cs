// File: Exceptions/TenantNotFoundException.cs
using System;

namespace Franz.Common.MultiTenancy.Exceptions
{
  public class TenantNotFoundException : Exception
  {
    public TenantNotFoundException(Guid tenantId)
        : base($"Tenant '{tenantId}' was not found.")
    {
      TenantId = tenantId;
    }

    public Guid TenantId { get; }
  }
}
