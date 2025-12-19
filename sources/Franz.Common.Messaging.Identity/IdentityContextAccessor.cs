using Franz.Common.Identity;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;

namespace Franz.Common.Messaging.Identity;

public class IdentityContextAccessor : IIdentityContextAccessor
{
  private readonly IMessageContextAccessor messageContextAccessor;

  public IdentityContextAccessor(IMessageContextAccessor messageContextAccessor)
  {
    this.messageContextAccessor = messageContextAccessor;
  }

  public string? GetCurrentEmail()
  {
    var headers = messageContextAccessor.Current?.Message.Headers;
    return headers != null && headers.TryGetIdentityEmail(out var v) ? v : null;
  }

  public Guid? GetCurrentId()
  {
    var headers = messageContextAccessor.Current?.Message.Headers;
    return headers != null && headers.TryGetMessageId(out var v) ? v : null;
  }

  public string? GetCurrentFullName()
  {
    var headers = messageContextAccessor.Current?.Message.Headers;
    return headers != null && headers.TryGetIdentityFullName(out var v) ? v : null;
  }

  public Guid? GetCurrentTenantId()
  {
    var headers = messageContextAccessor.Current?.Message.Headers;
    return headers != null && headers.TryGetTenantId(out var v) ? v : null;
  }

  public Guid? GetCurrentDomainId()
  {
    var headers = messageContextAccessor.Current?.Message.Headers;
    return headers != null && headers.TryGetDomainId(out var v) ? v : null;
  }

  public string[] GetCurrentRoles()
  {
    var headers = messageContextAccessor.Current?.Message.Headers;
    return headers != null && headers.TryGetIdentityRoles(out var v)
      ? v.ToArray()
      : Array.Empty<string>();
  }

  public FranzIdentityContext? GetCurrentIdentity()
  {
    var ctx = messageContextAccessor.Current;
    if (ctx == null) return null;

    var headers = ctx.Message.Headers;

    headers.TryGetIdentityId(out var userId);
    headers.TryGetIdentityEmail(out var email);
    headers.TryGetIdentityFullName(out var fullName);
    headers.TryGetTenantId(out var tenantId);
    headers.TryGetDomainId(out var domainId);
    headers.TryGetIdentityRoles(out var roles);

    return new FranzIdentityContext
    {
      UserId = userId,
      Email = email,
      FullName = fullName,
      TenantId = tenantId,
      DomainId = domainId,
      Roles = roles?.ToArray() ?? Array.Empty<string>()
    };
  }
}
