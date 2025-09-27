using Franz.Common.SSO.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Franz.Common.SSO.Extensions
{
  internal class FranzSsoStartupFilter : IStartupFilter
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
          enabledInteractive.Add("WsFederation");
        }

        if (_settings.Saml2?.Enabled == true)
        {
          logger.LogInformation("SAML2 SSO enabled.");
          enabledInteractive.Add("SAML2");
        }

        if (_settings.Oidc?.Enabled == true)
        {
          logger.LogInformation("OIDC SSO enabled.");
          enabledInteractive.Add("OIDC");
        }

        if (_settings.Keycloak?.Enabled == true)
        {
          logger.LogInformation("Keycloak SSO enabled.");
          enabledInteractive.Add("Keycloak");
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
