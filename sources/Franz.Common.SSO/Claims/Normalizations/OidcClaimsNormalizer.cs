using System.Security.Claims;

namespace Franz.Common.SSO.Claims.Normalization
{
  public class OidcClaimsNormalizer : IClaimsNormalizer
  {
    public bool CanHandle(ClaimsPrincipal principal)
        => principal.HasClaim(c => c.Type == "iss") || principal.Identity?.AuthenticationType?.Contains("OpenIdConnect") == true;

    public ClaimsPrincipal Normalize(ClaimsPrincipal principal)
    {
      var id = principal.Identity as ClaimsIdentity;
      if (id == null) return principal;

      // Ensure standard role & email/name are present
      if (!id.HasClaim(c => c.Type == ClaimTypes.Email))
      {
        var email = principal.FindFirst("email")?.Value;
        if (!string.IsNullOrEmpty(email)) id.AddClaim(new Claim(ClaimTypes.Email, email));
      }

      if (!id.HasClaim(c => c.Type == ClaimTypes.Name))
      {
        var name = principal.FindFirst("name")?.Value ?? principal.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrEmpty(name)) id.AddClaim(new Claim(ClaimTypes.Name, name));
      }

      // Many OIDC providers emit either "roles" or already ClaimTypes.Role
      var roles = principal.FindAll("roles").Select(r => r.Value).ToArray();
      foreach (var r in roles)
        if (!id.HasClaim(ClaimTypes.Role, r))
          id.AddClaim(new Claim(ClaimTypes.Role, r));

      return principal;
    }
  }
}
