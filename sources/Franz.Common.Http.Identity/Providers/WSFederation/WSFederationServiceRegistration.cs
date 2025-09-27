using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Http.Identity.Providers.WsFederation;

public static class WsFederationServiceCollectionExtensions
{
  public static IServiceCollection AddFranzWsFederationIdentity(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    var section = configuration.GetSection("FranzIdentity:WsFederation");
    var options = section.Get<WsFederationOptions>() ?? new WsFederationOptions();

    services.Configure<WsFederationOptions>(section);

    services.AddAuthentication(sharedOptions =>
    {
      sharedOptions.DefaultScheme = WsFederationDefaults.AuthenticationScheme;
      sharedOptions.DefaultChallengeScheme = WsFederationDefaults.AuthenticationScheme;
    })
    .AddWsFederation(o =>
    {
      o.MetadataAddress = options.MetadataAddress;
      o.Wtrealm = options.Wtrealm;
    });

    return services;
  }
}
