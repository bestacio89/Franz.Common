using Franz.Common.Http.Identity.Providers.Keycloak;
using Franz.Common.Http.Identity.Providers.OIDC;
using Franz.Common.Http.Identity.Providers.Saml2;
using Franz.Common.Http.Identity.Providers.WsFederation;
using Franz.Common.SSO.Claims.Normalization;
using Franz.Common.SSO.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Franz.Common.SSO.Extensions
{
  public static class SsoServiceCollectionExtensions
  {
    public static IServiceCollection AddFranzSsoIdentity(this IServiceCollection services, IConfiguration configuration)
    {
      // Bind settings
      var section = configuration.GetSection("FranzIdentity");
      var settings = section.Get<FranzSsoSettings>() ?? new FranzSsoSettings();

      // Always register the HTTP identity accessor
      services.AddHttpContextAccessor();

      // Register normalization pipeline
      services.AddScoped<IClaimsNormalizer, OidcClaimsNormalizer>();
      services.AddScoped<IClaimsNormalizer, KeycloakClaimsNormalizer>();
      services.AddScoped<IClaimsTransformation, CompositeClaimsTransformation>();

      // Register a startup callback to log once the container is built
      services.AddSingleton<IStartupFilter>(new FranzSsoStartupFilter(settings));

      return services;
    }

    private class FranzSsoStartupFilter : IStartupFilter
    {
      private readonly FranzSsoSettings _settings;

      public FranzSsoStartupFilter(FranzSsoSettings settings)
      {
        _settings = settings;
      }

      public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
      {
        return app =>
        {
          var logger = app.ApplicationServices.GetRequiredService<ILogger<FranzSsoStartupFilter>>();

          logger.LogInformation("Bootstrapping Franz SSO Identity...");
          logger.LogDebug("Interactive provider from config: {Provider}", _settings.InteractiveProvider);

          var enabledInteractive = new List<string>();

          if (_settings.WsFederation?.Enabled == true)
          {
            logger.LogInformation("WS-Federation SSO enabled.");
          }

          if (_settings.Saml2?.Enabled == true)
          {
            logger.LogInformation("SAML2 SSO enabled.");
          }

          if (_settings.Oidc?.Enabled == true)
          {
            logger.LogInformation("OIDC SSO enabled.");
          }

          if (_settings.Keycloak?.Enabled == true)
          {
            logger.LogInformation("Keycloak SSO enabled.");
          }

          if (enabledInteractive.Count > 1 && !_settings.AllowMultipleInteractiveProviders)
          {
            logger.LogError(
              "Multiple interactive SSO providers enabled but AllowMultipleInteractiveProviders=false. Enabled: {Providers}",
              string.Join(", ", enabledInteractive));

            throw new InvalidOperationException(
              $"Multiple interactive SSO providers enabled ({string.Join(", ", enabledInteractive)}).");
          }

          if (_settings.Jwt?.Enabled == true)
          {
            logger.LogInformation("JWT Bearer token support enabled.");
          }

          logger.LogInformation("Franz SSO bootstrap complete.");

          next(app);
        };
      }
    }
  }
}
