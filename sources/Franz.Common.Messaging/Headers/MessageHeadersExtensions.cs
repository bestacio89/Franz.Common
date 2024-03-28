
using Franz.Common.Errors;
using Franz.Common.Headers;

using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Headers;

public static class MessageHeadersExtensions
{
  public static bool TryGetMessageId(this MessageHeaders messageHeaders, out Guid messageId)
  {
    var result = messageHeaders.TryGetGuid(MessagingConstants.MessageId, out messageId);

    return result;
  }

  public static bool TryGetIdentityId(this MessageHeaders messageHeaders, out Guid userId)
  {
    var result = messageHeaders.TryGetGuid(HeaderConstants.UserId, out userId);

    return result;
  }

  public static bool TryGetIdentityEmail(this MessageHeaders messageHeaders, out string userEmail)
  {
    var result = messageHeaders.TryGetString(HeaderConstants.UserEmail, out userEmail);

    return result;
  }

  public static bool TryGetIdentityFullName(this MessageHeaders messageHeaders, out string userFullName)
  {
    var result = messageHeaders.TryGetString(HeaderConstants.UserFullName, out userFullName);

    return result;
  }

  public static bool TryGetIdentityRoles(this MessageHeaders messageHeaders, out IEnumerable<string> userRoles)
  {
    var result = messageHeaders.TryGetStringEnumerable(HeaderConstants.UserRoles, out userRoles);

    return result;
  }

  public static bool TryGetTenantId(this MessageHeaders messageHeaders, out Guid tenantId)
  {
    var result = messageHeaders.TryGetGuid(HeaderConstants.TenantId, out tenantId);

    return result;
  }

  public static bool TryGetDomainId(this MessageHeaders messageHeaders, out Guid domainId)
  {
    var result = messageHeaders.TryGetGuid(HeaderConstants.DomainId, out domainId);

    return result;
  }

  public static bool TryGetClassName(this MessageHeaders messageHeaders, out string classEventName)
  {
    var result = messageHeaders.TryGetString(MessagingConstants.ClassName, out classEventName);

    return result;
  }

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

    value = default!;
    if (result)
#pragma warning disable CS8601 // Possible null reference assignment.
      value = obj;
#pragma warning restore CS8601 // Possible null reference assignment.

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static string? GetString(this MessageHeaders messageHeaders, string key)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var check = messageHeaders.TryGetValue(key, out var obj);

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    string? result = null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    if (check)
      result = obj;

    return result;
  }

  public static bool TryGetStringEnumerable(this MessageHeaders messageHeaders, string key, out IEnumerable<string> value)
  {
    var result = messageHeaders.TryGetValue(key, out var obj);

    value = StringValues.Empty;
    if (result)
      value = obj;

    return result;
  }
}
