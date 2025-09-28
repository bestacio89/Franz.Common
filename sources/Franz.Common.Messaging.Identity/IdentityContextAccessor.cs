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
    if (messageContextAccessor.Current != null &&
        messageContextAccessor.Current.Message.Headers.TryGetIdentityEmail(out var userEmail))
      return userEmail;

    return null;
  }

  public Guid? GetCurrentId()
  {
    if (messageContextAccessor.Current != null &&
        messageContextAccessor.Current.Message.Headers.TryGetIdentityId(out var userId))
      return userId;

    return null;
  }

  public string? GetCurrentFullName()
  {
    if (messageContextAccessor.Current != null &&
        messageContextAccessor.Current.Message.Headers.TryGetIdentityFullName(out var userFullName))
      return userFullName;

    return null;
  }

  public Guid? GetCurrentTenantId()
  {
    if (messageContextAccessor.Current != null &&
        messageContextAccessor.Current.Message.Headers.TryGetTenantId(out var tenantId))
      return tenantId;

    return null;
  }

  public Guid? GetCurrentDomainId()
  {
    if (messageContextAccessor.Current != null &&
        messageContextAccessor.Current.Message.Headers.TryGetDomainId(out var domainId))
      return domainId;

    return null;
  }

  public string[] GetCurrentRoles()
  {
    if (messageContextAccessor.Current != null &&
        messageContextAccessor.Current.Message.Headers.TryGetIdentityRoles(out var roles))
      return roles.ToArray();

    return Enumerable.Empty<string>().ToArray();
  }

  public FranzIdentityContext? GetCurrentIdentity()
  {
    if (messageContextAccessor.Current == null) return null;

    var headers = messageContextAccessor.Current.Message.Headers;

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
