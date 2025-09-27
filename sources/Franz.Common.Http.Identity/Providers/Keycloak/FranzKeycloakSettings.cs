namespace Franz.Common.Http.Identity.Providers.Keycloak
{
  public class FranzKeycloakSettings
  {
    public bool Enabled { get; set; }
    public string Authority { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string CallbackPath { get; set; } = "/signin-keycloak";
    public bool SaveTokens { get; set; } = true;

    // Claim mapping toggles
    public bool MapRealmRolesToClaims { get; set; } = true;
    public bool MapResourceRolesToClaims { get; set; } = true;
  }
}
