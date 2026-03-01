using Franz.Common.Errors;
using Franz.Common.Headers;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Headers;

public static class MessageHeadersExtensions
{
  public static bool TryGetMessageId(this MessageHeaders messageHeaders, out Guid messageId)
      => messageHeaders.TryGetGuid(MessagingConstants.MessageId, out messageId);

  public static bool TryGetIdentityId(this MessageHeaders messageHeaders, out Guid userId)
      => messageHeaders.TryGetGuid(HeaderConstants.UserId, out userId);

  public static bool TryGetIdentityEmail(this MessageHeaders messageHeaders, out string userEmail)
      => messageHeaders.TryGetString(HeaderConstants.UserEmail, out userEmail);

  public static bool TryGetIdentityFullName(this MessageHeaders messageHeaders, out string userFullName)
      => messageHeaders.TryGetString(HeaderConstants.UserFullName, out userFullName);

  public static bool TryGetIdentityRoles(this MessageHeaders messageHeaders, out IEnumerable<string> userRoles)
      => messageHeaders.TryGetStringEnumerable(HeaderConstants.UserRoles, out userRoles);

  public static bool TryGetTenantId(this MessageHeaders messageHeaders, out Guid tenantId)
      => messageHeaders.TryGetGuid(HeaderConstants.TenantId, out tenantId);

  public static bool TryGetDomainId(this MessageHeaders messageHeaders, out Guid domainId)
      => messageHeaders.TryGetGuid(HeaderConstants.DomainId, out domainId);

  public static bool TryGetClassName(this MessageHeaders messageHeaders, out string classEventName)
      => messageHeaders.TryGetString(MessagingConstants.ClassName, out classEventName);

  public static string GetClassName(this MessageHeaders messageHeaders)
  {
    var check = messageHeaders.TryGetClassName(out var result);
    return !check
        ? throw new TechnicalException(string.Format(Common.Headers.Properties.Resources.HeaderNotFoundException, MessagingConstants.ClassName))
        : result;
  }

  public static bool TryGetGuid(this MessageHeaders messageHeaders, string key, out Guid value)
  {
    var result = messageHeaders.TryGetValue(key, out var obj);
    value = default;
    if (result && Guid.TryParse(obj, out var id))
      value = id;
    return result;
  }

  public static bool TryGetString(this MessageHeaders messageHeaders, string key, out string value)
  {
    var result = messageHeaders.TryGetValue(key, out var obj);
    value = obj!;
    return result;
  }

  public static string? GetString(this MessageHeaders messageHeaders, string key)
  {
    return messageHeaders.TryGetValue(key, out var obj) ? (string?)obj : null;
  }

  public static bool TryGetStringEnumerable(this MessageHeaders messageHeaders, string key, out IEnumerable<string> value)
  {
    var result = messageHeaders.TryGetValue(key, out var obj);
    value = result ? obj : StringValues.Empty;
    return result;
  }

  // --- Setters ---
  public static void SetTenantId(this MessageHeaders messageHeaders, Guid tenantId) => messageHeaders[HeaderConstants.TenantId] = tenantId.ToString();
  public static void SetDomainId(this MessageHeaders messageHeaders, Guid domainId) => messageHeaders[HeaderConstants.DomainId] = domainId.ToString();
  public static void SetIdentityId(this MessageHeaders messageHeaders, Guid userId) => messageHeaders[HeaderConstants.UserId] = userId.ToString();
  public static void SetIdentityEmail(this MessageHeaders messageHeaders, string email) => messageHeaders[HeaderConstants.UserEmail] = email;
  public static void SetIdentityFullName(this MessageHeaders messageHeaders, string fullName) => messageHeaders[HeaderConstants.UserFullName] = fullName;
  public static void SetIdentityRoles(this MessageHeaders messageHeaders, IEnumerable<string> roles) => messageHeaders[HeaderConstants.UserRoles] = new StringValues(roles.ToArray());
}