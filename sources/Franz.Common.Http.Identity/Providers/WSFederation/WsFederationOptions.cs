namespace Franz.Common.Http.Identity.Providers.WsFederation;

public class WsFederationOptions
{
  public bool Enabled { get; set; }
  public string MetadataAddress { get; set; } = string.Empty;
  public string Wtrealm { get; set; } = string.Empty;
}
