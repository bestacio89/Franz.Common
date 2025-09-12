using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy.Accessors;

public class TenantContextAccessor : ITenantContextAccessor
{
  private readonly IMessageContextAccessor _messageContextAccessor;

  public TenantContextAccessor(IMessageContextAccessor messageContextAccessor)
  {
    _messageContextAccessor = messageContextAccessor;
  }

  public Guid? GetCurrentId()
  {
    if (_messageContextAccessor.Current?.Message.Headers.TryGetTenantId(out var tenantId) == true)
      return tenantId;

    return null;
  }

  public void SetCurrentId(Guid tenantId)
  {
    _messageContextAccessor.Current?.Message.Headers.SetTenantId(tenantId);
  }

  public Guid? GetCurrentTenantId()
  {
    if (_messageContextAccessor.Current?.Message.Headers.TryGetDomainId(out var domainId) == true)
      return domainId;

    return null;
  }

  public void SetCurrentTenantId(Guid domainId)
  {
    _messageContextAccessor.Current?.Message.Headers.SetDomainId(domainId);
  }
}
