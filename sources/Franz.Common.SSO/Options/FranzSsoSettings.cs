using Franz.Common.Http.Identity.Providers.Keycloak;
using Franz.Common.Http.Identity.Providers.OIDC;
using Franz.Common.Http.Identity.Providers.Saml2;
using Franz.Common.Http.Identity.Providers.WsFederation;

namespace Franz.Common.SSO.Options
{
  public class FranzSsoSettings
  {
    public bool AllowMultipleInteractiveProviders { get; set; }

    public WsFederationOptions? WsFederation { get; set; }
    public OidcOptions? Oidc { get; set; }
    public FranzSaml2Settings? Saml2 { get; set; }
    public FranzKeycloakSettings? Keycloak { get; set; }
    public FranzJwtSettings? Jwt { get; set; }

    // Optional: choose which provider should be considered "default"
    public string? InteractiveProvider { get; set; }
  }
}
