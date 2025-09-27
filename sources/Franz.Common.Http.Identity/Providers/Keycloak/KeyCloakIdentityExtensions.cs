using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Franz.Common.Http.Identity.Providers.Keycloak
{
  public static class KeycloakIdentityExtensions
  {
    public static AuthenticationBuilder AddFranzKeycloakIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
      var section = configuration.GetSection("FranzIdentity:Keycloak");
      services.Configure<FranzKeycloakSettings>(section);

      var settings = section.Get<FranzKeycloakSettings>()!;

      // Register claims transformer
      services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformer>();

      return services.AddAuthentication(options =>
      {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
      })
      .AddCookie()
      .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
      {
        options.Authority = settings.Authority;
        options.ClientId = settings.ClientId;
        options.ClientSecret = settings.ClientSecret;
        options.ResponseType = "code";
        options.SaveTokens = settings.SaveTokens;
        options.CallbackPath = settings.CallbackPath;

        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
      });
    }
  }
}
