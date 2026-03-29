#nullable enable
using Franz.Common.Errors;
using Franz.Common.Headers;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Messaging.Headers;

public class MessageHeaders : Dictionary<string, string[]>
{
  public MessageHeaders() : base(StringComparer.OrdinalIgnoreCase) { }

  public MessageHeaders(IEnumerable<KeyValuePair<string, string[]>> values)
      : base(StringComparer.OrdinalIgnoreCase)
  {
    foreach (var value in values)
      this[value.Key] = value.Value;
  }

  // Senior Note: Added to simplify Kafka/Rabbit header mapping
  public void SetHeader(string key, string value) => this[key] = [value];

  public bool TryGetString(string key, [NotNullWhen(true)] out string? value)
  {
    value = default;
    if (!TryGetValue(key, out var values) || values.Length == 0)
      return false;

    value = values[0];
    return !string.IsNullOrWhiteSpace(value);
  }

  public bool TryGetGuid(string key, out Guid value)
  {
    value = default;
    return TryGetString(key, out var s) && Guid.TryParse(s, out value);
  }

  public bool TryGetStringEnumerable(string key, out IEnumerable<string> values)
  {
    if (!TryGetValue(key, out var stringArray))
    {
      values = Enumerable.Empty<string>();
      return false;
    }
    values = stringArray.Where(v => !string.IsNullOrWhiteSpace(v));
    return true;
  }

  public void SetTenantId(Guid tenantId) => SetHeader(HeaderConstants.TenantId, tenantId.ToString());
  public void SetUserId(Guid userId) => SetHeader(HeaderConstants.UserId, userId.ToString());
}