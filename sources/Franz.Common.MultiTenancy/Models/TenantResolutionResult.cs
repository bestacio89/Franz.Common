using System;
using Franz.Common.MultiTenancy.Models;

#nullable enable
namespace Franz.Common.MultiTenancy.TenantResolution
{
  /// <summary>
  /// Result returned by resolvers/pipeline. Contains the resolved tenant if successful.
  /// </summary>
  public sealed class TenantResolutionResult
  {
    private TenantResolutionResult(TenantInfo? tenant, TenantResolutionSource source, bool succeeded, string? message = null)
    {
      Tenant = tenant;
      Source = source;
      Succeeded = succeeded;
      Message = message;
    }

    /// <summary>The resolved tenant (null if resolution failed).</summary>
    public TenantInfo? Tenant { get; }

    /// <summary>The source of tenant resolution (e.g., Header, QueryString, JWT, Message).</summary>
    public TenantResolutionSource Source { get; }

    /// <summary>True if tenant resolution was successful.</summary>
    public bool Succeeded { get; }

    /// <summary>Optional message providing extra context (e.g., failure reason).</summary>
    public string? Message { get; }

    /// <summary>Create a success result.</summary>
    public static TenantResolutionResult Success(TenantInfo tenant, TenantResolutionSource source, string? message = null) =>
        new TenantResolutionResult(tenant, source, true, message);

    /// <summary>Create a failed result.</summary>
    public static TenantResolutionResult FailedResult(TenantResolutionSource source, string? message = null) =>
        new TenantResolutionResult(null, source, false, message);
  }
}
