using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.SSO.Options;
public class FranzJwtSettings
{
  public bool Enabled { get; set; }
  public string Authority { get; set; } = string.Empty;
  public string Audience { get; set; } = string.Empty;
  public bool RequireHttps { get; set; } = true;
  public bool ValidateIssuer { get; set; } = true;
}