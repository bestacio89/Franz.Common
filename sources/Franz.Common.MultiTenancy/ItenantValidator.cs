// File: ITenantValidator.cs
using System;
using System.Threading.Tasks;

namespace Franz.Common.MultiTenancy
{
  /// <summary>
  /// Optional validation before considering a tenant valid for request processing.
  /// Implementations can enforce tenant state (active/paused/disabled), expiry, billing, etc.
  /// </summary>
  public interface ITenantValidator
  {
    Task<bool> IsValidAsync(Guid tenantId);
  }
}