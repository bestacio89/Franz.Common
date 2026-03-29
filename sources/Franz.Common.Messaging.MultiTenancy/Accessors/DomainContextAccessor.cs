#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy.Accessors;

/// <summary>
/// Manages Domain ID metadata within the current message context.
/// Senior Note: Updated to support serializable string[] headers and eliminate legacy extension dependencies.
/// </summary>
public sealed class DomainContextAccessor(IMessageContextAccessor messageContextAccessor)
    : IDomainContextAccessor
{
  private readonly IMessageContextAccessor _messageContextAccessor = messageContextAccessor;

  public Guid? GetCurrentId() => GetCurrentDomainId();

  public void SetCurrentId(Guid domainId) => SetCurrentDomainId(domainId);

  public Guid? GetCurrentDomainId()
  {
    var headers = _messageContextAccessor.Current?.Message.Headers;

    // SENIOR FIX: Explicitly resolve the DomainId from the string array.
    if (headers != null &&
        headers.TryGetValue(HeaderConstants.DomainId, out var values) &&
        values.Length > 0 &&
        Guid.TryParse(values[0], out var domainId))
    {
      return domainId;
    }

    return null;
  }

  public void SetCurrentDomainId(Guid domainId)
  {
    var headers = _messageContextAccessor.Current?.Message.Headers;
    if (headers != null)
    {
      // SENIOR FIX: Wrap the Guid in a string array for JSON-safe wire transport.
      headers[HeaderConstants.DomainId] = new[] { domainId.ToString() };
    }
  }
}