using System.Security.Claims;
using Franz.Common.Identity;

namespace Franz.Common.SSO.Claims.Normalization
{
  public interface IClaimsNormalizer
  {
    bool CanHandle(ClaimsPrincipal principal);
    // Returns an updated principal (add/normalize claims); you can also feed FranzIdentityContext downstream
    ClaimsPrincipal Normalize(ClaimsPrincipal principal);
  }
}
