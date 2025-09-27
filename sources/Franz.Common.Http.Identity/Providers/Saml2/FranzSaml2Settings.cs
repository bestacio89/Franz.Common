namespace Franz.Common.Http.Identity.Providers.Saml2
{
  public class FranzSaml2Settings
  {
    public bool Enabled { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-saml2";
    public string IdpMetadata { get; set; } = string.Empty;

    // Franz-style toggles
    public bool EnforceNameIdPolicy { get; set; } = true;
    public bool EnableRelayTrace { get; set; } = false;
  }
}
