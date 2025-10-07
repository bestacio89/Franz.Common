using Franz.Common.Identity;

public interface IIdentityContextAccessor
{
  string? GetCurrentEmail();
  Guid? GetCurrentId();
  string? GetCurrentFullName();
  Guid? GetCurrentTenantId();
  Guid? GetCurrentDomainId();
  string[]? GetCurrentRoles();

  FranzIdentityContext? GetCurrentIdentity(); // ✅ make nullable
}
