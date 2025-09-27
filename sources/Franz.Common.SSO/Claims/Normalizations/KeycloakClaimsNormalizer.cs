using System.Security.Claims;
using System.Text.Json;

namespace Franz.Common.SSO.Claims.Normalization
{
  public class KeycloakClaimsNormalizer : IClaimsNormalizer
  {
    public bool CanHandle(ClaimsPrincipal principal)
        => principal.HasClaim(c => c.Type == "iss" && c.Value.Contains("realms"));

    public ClaimsPrincipal Normalize(ClaimsPrincipal principal)
    {
      var id = principal.Identity as ClaimsIdentity;
      if (id == null) return principal;

      // realm_access.roles
      var realmAccess = principal.FindFirst("realm_access")?.Value;
      if (!string.IsNullOrWhiteSpace(realmAccess))
      {
        try
        {
          var doc = JsonDocument.Parse(realmAccess);
          if (doc.RootElement.TryGetProperty("roles", out var rolesEl))
          {
            foreach (var role in rolesEl.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x))!)
              if (!id.HasClaim(ClaimTypes.Role, role!))
                id.AddClaim(new Claim(ClaimTypes.Role, role!));
          }
        }
        catch { /* ignore */ }
      }

      // resource_access.{client}.roles
      var resourceAccess = principal.FindFirst("resource_access")?.Value;
      if (!string.IsNullOrWhiteSpace(resourceAccess))
      {
        try
        {
          var doc = JsonDocument.Parse(resourceAccess);
          foreach (var app in doc.RootElement.EnumerateObject())
          {
            if (app.Value.TryGetProperty("roles", out var rolesEl))
            {
              foreach (var role in rolesEl.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x))!)
                if (!id.HasClaim(ClaimTypes.Role, role!))
                  id.AddClaim(new Claim(ClaimTypes.Role, role!));
            }
          }
        }
        catch { /* ignore */ }
      }

      return principal;
    }
  }
}
