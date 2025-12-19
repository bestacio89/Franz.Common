using Franz.Common.Errors;
using Franz.Common.Headers;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Headers;

public class MessageHeaders : Dictionary<string, StringValues>
{
  public MessageHeaders(StringComparer ordinalIgnoreCase) { }

  public MessageHeaders(IEnumerable<KeyValuePair<string, StringValues>> values)
  {
    foreach (var value in values)
      Add(value.Key, value.Value);
  }

  public MessageHeaders()
  {
  }

  // ----------------------------
  // Core access helpers
  // ----------------------------

  public bool TryGetString(string key, out string value)
  {
    value = default!;

    if (!TryGetValue(key, out var values))
      return false;

    value = values.FirstOrDefault()!;
    return value != null;
  }

  public bool TryGetGuid(string key, out Guid value)
  {
    value = default;

    if (!TryGetValue(key, out var values))
      return false;

    return Guid.TryParse(values.FirstOrDefault(), out value);
  }

  public bool TryGetStringEnumerable(string key, out IEnumerable<string> values)
  {
    if (!TryGetValue(key, out var stringValues))
    {
      values = Enumerable.Empty<string>();
      return false;
    }

    values = stringValues.Where(v => !string.IsNullOrWhiteSpace(v));
    return true;
  }

  // ----------------------------
  // Identity helpers
  // ----------------------------

  public bool TryGetIdentityId(out Guid userId) =>
    TryGetGuid(HeaderConstants.UserId, out userId);

  public bool TryGetIdentityEmail(out string email) =>
    TryGetString(HeaderConstants.UserEmail, out email);

  public bool TryGetIdentityFullName(out string fullName) =>
    TryGetString(HeaderConstants.UserFullName, out fullName);

  public bool TryGetIdentityRoles(out IEnumerable<string> roles) =>
    TryGetStringEnumerable(HeaderConstants.UserRoles, out roles);

  public bool TryGetTenantId(out Guid tenantId) =>
    TryGetGuid(HeaderConstants.TenantId, out tenantId);

  public bool TryGetDomainId(out Guid domainId) =>
    TryGetGuid(HeaderConstants.DomainId, out domainId);

  // ----------------------------
  // Mandatory getters
  // ----------------------------

  public string GetRequiredString(string key)
  {
    if (!TryGetString(key, out var value))
      throw new TechnicalException(
        $"Required header '{key}' was not found");

    return value;
  }

  // ----------------------------
  // Setters (invariants)
  // ----------------------------

  public void SetTenantId(Guid tenantId) =>
    this[HeaderConstants.TenantId] = tenantId.ToString();

  public void SetDomainId(Guid domainId) =>
    this[HeaderConstants.DomainId] = domainId.ToString();

  public void SetIdentityId(Guid userId) =>
    this[HeaderConstants.UserId] = userId.ToString();

  public void SetIdentityEmail(string email) =>
    this[HeaderConstants.UserEmail] = email;

  public void SetIdentityFullName(string fullName) =>
    this[HeaderConstants.UserFullName] = fullName;

  public void SetIdentityRoles(IEnumerable<string> roles) =>
    this[HeaderConstants.UserRoles] = new StringValues(roles.ToArray());
}
