using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;

namespace Franz.Common.Http.Identity.Providers.Saml2
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddFranzSaml2Identity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
      var settings = new FranzSaml2Settings();
      configuration.GetSection("FranzIdentity:Saml2").Bind(settings);

      services.AddAuthentication(options =>
      {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = Saml2Defaults.Scheme;
      })
      .AddCookie("Cookies")
      .AddSaml2(Saml2Defaults.Scheme, opts =>
      {
        // SP setup
        opts.SPOptions.EntityId = new Sustainsys.Saml2.Metadata.EntityId(settings.EntityId);
        opts.SPOptions.ReturnUrl = new Uri(settings.CallbackPath, UriKind.Relative);

        // IdP setup
        opts.IdentityProviders.Add(new Sustainsys.Saml2.IdentityProvider(
                  new Sustainsys.Saml2.Metadata.EntityId(settings.IdpMetadata),
                  opts.SPOptions)
        {
          MetadataLocation = settings.IdpMetadata,
          LoadMetadata = true
        });

        // Notifications
        Saml2NotificationsConfigurator.Configure(opts, settings);
      });

      return services;
    }
  }
}
