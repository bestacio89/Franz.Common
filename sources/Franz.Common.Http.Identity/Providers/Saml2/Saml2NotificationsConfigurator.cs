using System;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Saml2P; // required for NameIdPolicy if available
using Sustainsys.Saml2.WebSso; // for CommandResult / Saml2Response

namespace Franz.Common.Http.Identity.Providers.Saml2
{
  public static class Saml2NotificationsConfigurator
  {
    public static void Configure(Saml2Options options, FranzSaml2Settings settings)
    {
      if (options == null) throw new ArgumentNullException(nameof(options));
      if (settings == null) throw new ArgumentNullException(nameof(settings));

      options.Notifications.AuthenticationRequestCreated = (request, idp, relayData) =>
      {
       

        if (settings.EnableRelayTrace)
        {
          relayData["FranzTrace"] = Guid.NewGuid().ToString();
        }
      };

      // Match your installed version signature: Action<CommandResult, Saml2Response>
      options.Notifications.AcsCommandResultCreated = (result, response) =>
      {
        // Optional: inspect result / response
      };

      options.Notifications.LogoutCommandResultCreated = result =>
      {
        // Optional: log logout events
      };
    }
  }
}
