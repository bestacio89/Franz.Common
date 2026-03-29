#nullable enable
using Franz.Common.Errors;
using Franz.Common.Headers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;

namespace Franz.Common.Messaging.Headers;

public static class MessageHeadersExtensions
{
  public static bool TryGetMessageId(this IDictionary<string, string[]> messageHeaders, out Guid messageId)
      => messageHeaders.TryGetGuid(MessagingConstants.MessageId, out messageId);

  public static bool TryGetIdentityId(this IDictionary<string, string[]> messageHeaders, out Guid userId)
      => messageHeaders.TryGetGuid(HeaderConstants.UserId, out userId);

  public static bool TryGetIdentityEmail(this IDictionary<string, string[]> messageHeaders, [NotNullWhen(true)] out string? userEmail)
      => messageHeaders.TryGetString(HeaderConstants.UserEmail, out userEmail);

  public static bool TryGetIdentityFullName(this IDictionary<string, string[]> messageHeaders, [NotNullWhen(true)] out string? userFullName)
      => messageHeaders.TryGetString(HeaderConstants.UserFullName, out userFullName);

  public static bool TryGetIdentityRoles(this IDictionary<string, string[]> messageHeaders, out IEnumerable<string> userRoles)
      => messageHeaders.TryGetStringEnumerable(HeaderConstants.UserRoles, out userRoles);

  public static bool TryGetTenantId(this IDictionary<string, string[]> messageHeaders, out Guid tenantId)
      => messageHeaders.TryGetGuid(HeaderConstants.TenantId, out tenantId);

    public static bool TryGetDomainId(this IDictionary<string, string[]> messageHeaders, out Guid domainId)
        => messageHeaders.TryGetGuid(HeaderConstants.DomainId, out domainId);

  public static bool TryGetClassName(this IDictionary<string, string[]> messageHeaders, [NotNullWhen(true)] out string? classEventName)
      => messageHeaders.TryGetString(MessagingConstants.ClassName, out classEventName);

  public static string GetClassName(this IDictionary<string, string[]> messageHeaders)
  {
    if (!messageHeaders.TryGetClassName(out var result))
      throw new TechnicalException(string.Format(Common.Headers.Properties.Resources.HeaderNotFoundException, MessagingConstants.ClassName));
    return result;
  }

  public static bool TryGetGuid(this IDictionary<string, string[]> messageHeaders, string key, out Guid value)
  {
    value = default;
    return messageHeaders.TryGetString(key, out var s) && Guid.TryParse(s, out value);
  }

  public static bool TryGetString(this IDictionary<string, string[]> messageHeaders, string key, [NotNullWhen(true)] out string? value)
  {
    value = default;
    // FIX: Extract the first element from the string array
    if (messageHeaders.TryGetValue(key, out var values) && values.Length > 0)
    {
      value = values[0];
      return true;
    }
    return false;
  }

  public static string? GetString(this IDictionary<string, string[]> messageHeaders, string key)
  {
    return messageHeaders.TryGetString(key, out var value) ? value : null;
  }

  public static bool TryGetStringEnumerable(this IDictionary<string, string[]> messageHeaders, string key, out IEnumerable<string> value)
  {
    if (messageHeaders.TryGetValue(key, out var obj))
    {
      value = obj;
      return true;
    }
    value = Array.Empty<string>();
    return false;
  }

  // --- Setters (The "Wrap in Array" Fixes) ---

  public static void SetTenantId(this IDictionary<string, string[]> messageHeaders, Guid tenantId)
      => messageHeaders[HeaderConstants.TenantId] = [tenantId.ToString()];

  public static void SetDomainId(this IDictionary<string, string[]> messageHeaders, Guid domainId)
      => messageHeaders[HeaderConstants.DomainId] = [domainId.ToString()];

  public static void SetIdentityId(this IDictionary<string, string[]> messageHeaders, Guid userId)
      => messageHeaders[HeaderConstants.UserId] = [userId.ToString()];

  public static void SetIdentityEmail(this IDictionary<string, string[]> messageHeaders, string email)
      => messageHeaders[HeaderConstants.UserEmail] = [email];

  public static void SetIdentityFullName(this IDictionary<string, string[]> messageHeaders, string fullName)
      => messageHeaders[HeaderConstants.UserFullName] = [fullName];

  public static void SetIdentityRoles(this IDictionary<string, string[]> messageHeaders, IEnumerable<string> roles)
      => messageHeaders[HeaderConstants.UserRoles] = roles.ToArray();
}