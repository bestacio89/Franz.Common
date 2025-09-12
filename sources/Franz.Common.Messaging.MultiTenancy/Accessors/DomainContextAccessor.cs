using Franz.Common.MultiTenancy;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;

namespace Franz.Common.Messaging.MultiTenancy.Accessors;

public class DomainContextAccessor : IDomainContextAccessor
{
  private readonly IMessageContextAccessor _messageContextAccessor;

  public DomainContextAccessor(IMessageContextAccessor messageContextAccessor)
  {
    _messageContextAccessor = messageContextAccessor;
  }

  public Guid? GetCurrentId()
  {
    if (_messageContextAccessor.Current?.Message.Headers.TryGetDomainId(out var domainId) == true)
      return domainId;

    return null;
  }

  public void SetCurrentId(Guid domainId)
  {
    _messageContextAccessor.Current?.Message.Headers.SetDomainId(domainId);
  }

  public Guid? GetCurrentDomainId()
  {
    if (_messageContextAccessor.Current?.Message.Headers.TryGetDomainId(out var domainId) == true)
      return domainId;

    return null;
  }

  public void SetCurrentDomainId(Guid domainId)
  {
    _messageContextAccessor.Current?.Message.Headers.SetDomainId(domainId);
  }
}
