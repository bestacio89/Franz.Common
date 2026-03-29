#nullable enable
using Franz.Common.Headers;
using Franz.Common.Identity;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;

namespace Franz.Common.Messaging.Identity;

/// <summary>
/// Extracts identity information from the current message context headers.
/// Senior Note: Updated to handle the new IDictionary<string, string[]> header contract.
/// </summary>
public sealed class IdentityContextAccessor(IMessageContextAccessor messageContextAccessor)
    : IIdentityContextAccessor
{
  public string? GetCurrentEmail() =>
      TryGetHeader(HeaderConstants.UserEmail, out var v) ? v : null;

  public Guid? GetCurrentId() =>
      TryGetHeaderGuid(HeaderConstants.UserId, out var v) ? v : null;

  public string? GetCurrentFullName() =>
      TryGetHeader(HeaderConstants.UserFullName, out var v) ? v : null;

  public Guid? GetCurrentTenantId() =>
      TryGetHeaderGuid(HeaderConstants.TenantId, out var v) ? v : null;

  public Guid? GetCurrentDomainId() =>
      TryGetHeaderGuid(HeaderConstants.DomainId, out var v) ? v : null;

  public string[] GetCurrentRoles()
  {
    var headers = messageContextAccessor.Current?.Message.Headers;
    if (headers != null && headers.TryGetValue(HeaderConstants.UserRoles, out var roles))
    {
      return roles; // Already a string[]
    }
    return Array.Empty<string>();
  }

  public FranzIdentityContext? GetCurrentIdentity()
  {
    var ctx = messageContextAccessor.Current;
    if (ctx == null) return null;

    return new FranzIdentityContext
    {
      UserId = GetCurrentId(),
      Email = GetCurrentEmail(),
      FullName = GetCurrentFullName(),
      TenantId = GetCurrentTenantId(),
      DomainId = GetCurrentDomainId(),
      Roles = GetCurrentRoles()
    };
  }

  // --- INTERNAL HELPERS FOR NEW CONTRACT ---

  private bool TryGetHeader(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
  {
    value = null;
    var headers = messageContextAccessor.Current?.Message.Headers;
    if (headers != null && headers.TryGetValue(key, out var values) && values.Length > 0)
    {
      value = values[0];
      return true;
    }
    return false;
  }

  private bool TryGetHeaderGuid(string key, out Guid? value)
  {
    value = null;
    if (TryGetHeader(key, out var s) && Guid.TryParse(s, out var guid))
    {
      value = guid;
      return true;
    }
    return false;
  }
}