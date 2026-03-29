#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy.Accessors;

/// <summary>
/// Manages Tenant ID metadata within the current message context.
/// Senior Note: Fixed logic bug where TenantId was pulling from DomainId; aligned with string[] contract.
/// </summary>
public sealed class TenantContextAccessor(IMessageContextAccessor messageContextAccessor)
    : ITenantContextAccessor
{
  private readonly IMessageContextAccessor _messageContextAccessor = messageContextAccessor;

  public Guid? GetCurrentId() => GetCurrentTenantId();

  public void SetCurrentId(Guid tenantId) => SetCurrentTenantId(tenantId);

  public Guid? GetCurrentTenantId()
  {
    var headers = _messageContextAccessor.Current?.Message.Headers;

    // SENIOR FIX: Corrected mapping to use TenantId instead of DomainId.
    // Explicitly resolve from the string array for JSON-safe messaging.
    if (headers != null &&
        headers.TryGetValue(HeaderConstants.TenantId, out var values) &&
        values.Length > 0 &&
        Guid.TryParse(values[0], out var tenantId))
    {
      return tenantId;
    }

    return null;
  }

  public void SetCurrentTenantId(Guid tenantId)
  {
    var headers = _messageContextAccessor.Current?.Message.Headers;
    if (headers != null)
    {
      // SENIOR FIX: Ensure the ID is wrapped in a string array for wire-format consistency.
      headers[HeaderConstants.TenantId] = new[] { tenantId.ToString() };
    }
  }
}