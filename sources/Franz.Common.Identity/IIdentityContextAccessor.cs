namespace Franz.Common.Identity
{
  public interface IIdentityContextAccessor
  {
    Guid? GetCurrentId();
    string? GetCurrentFullName();
    string? GetCurrentEmail();
    string[]? GetCurrentRoles();

    Guid? GetCurrentTenantId();
    Guid? GetCurrentDomainId();

    FranzIdentityContext GetCurrentIdentity();
  }
}
