using Franz.Common.Identity;

namespace Franz.Common.Identity.Testing;

public class FakeIdentityContextAccessor : IIdentityContextAccessor
{
  private readonly FranzIdentityContext identity;

  public FakeIdentityContextAccessor(FranzIdentityContext identity)
  {
    this.identity = identity;
  }

  public Guid? GetCurrentId() => identity.UserId;
  public string? GetCurrentFullName() => identity.FullName;
  public string? GetCurrentEmail() => identity.Email;
  public string[]? GetCurrentRoles() => identity.Roles;

  public Guid? GetCurrentTenantId() => identity.TenantId;
  public Guid? GetCurrentDomainId() => identity.DomainId;

  public FranzIdentityContext GetCurrentIdentity() => identity;
}
