using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Franz.Common.Http.Identity.Providers.Keycloak
{
  public class KeycloakClaimsTransformer : IClaimsTransformation
  {
    private readonly FranzKeycloakSettings _settings;

    public KeycloakClaimsTransformer(IOptions<FranzKeycloakSettings> settings)
    {
      _settings = settings.Value;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
      var identity = (ClaimsIdentity)principal.Identity!;

      // Map realm roles → ClaimTypes.Role
      if (_settings.MapRealmRolesToClaims)
      {
        var realmRoles = principal.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrWhiteSpace(realmRoles))
        {
          foreach (var role in ParseRoles(realmRoles))
          {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
          }
        }
      }

      // Map resource roles → ClaimTypes.Role
      if (_settings.MapResourceRolesToClaims)
      {
        var resourceAccess = principal.FindFirst("resource_access")?.Value;
        if (!string.IsNullOrWhiteSpace(resourceAccess))
        {
          foreach (var role in ParseRoles(resourceAccess))
          {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
          }
        }
      }

      return Task.FromResult(principal);
    }

    private static IEnumerable<string> ParseRoles(string raw)
    {
      // Keycloak sends roles as JSON blobs → quick & safe parse
      try
      {
        var roles = System.Text.Json.JsonDocument.Parse(raw)
            .RootElement
            .EnumerateObject()
            .SelectMany(obj => obj.Value.GetProperty("roles").EnumerateArray())
            .Select(r => r.GetString()!)
            .Distinct();

        return roles;
      }
      catch
      {
        return Enumerable.Empty<string>();
      }
    }
  }
}
