using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Http.Identity.Providers.OIDC;
public class OidcOptions
{
  public bool Enabled { get; set; }
  public string Authority { get; set; } = string.Empty;
  public string ClientId { get; set; } = string.Empty;
  public string ClientSecret { get; set; } = string.Empty;
}