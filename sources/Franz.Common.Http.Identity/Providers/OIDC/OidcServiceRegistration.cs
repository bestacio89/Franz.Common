using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Http.Identity.Providers.OIDC;
public static class OidcServiceCollectionExtensions
{
  public static IServiceCollection AddFranzOidcIdentity(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    var section = configuration.GetSection("FranzIdentity:Oidc");
    var options = section.Get<OidcOptions>() ?? new OidcOptions();

    services.Configure<OidcOptions>(section);

    services.AddAuthentication(o =>
    {
      o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
      o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, o =>
    {
      o.Authority = options.Authority;
      o.ClientId = options.ClientId;
      o.ClientSecret = options.ClientSecret;
      o.ResponseType = "code";
    });

    return services;
  }
}