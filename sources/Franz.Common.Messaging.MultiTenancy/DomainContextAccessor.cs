using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy;

public class DomainContextAccessor : IDomainContextAccessor
{
  private readonly IMessageContextAccessor messageContextAccessor;

  public DomainContextAccessor(IMessageContextAccessor messageContextAccessor)
  {
    this.messageContextAccessor = messageContextAccessor;
  }

  public virtual Guid? GetCurrentId()
  {
    Guid? result = null;

    if (messageContextAccessor.Current != null && messageContextAccessor.Current.Message.Headers.TryGetDomainId(out var domainId))
      result = domainId;

    return result;
  }
}
