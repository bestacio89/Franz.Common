
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.EntityFramework;
using Franz.Common.SSO;
using Franz.Common.SSO.Interfaces;

namespace Franz.Common.SSO.Extensions;
public static class SsoServiceRegistration
{
  public static void AddSsoManager<TDbContext>(this IServiceCollection services) where TDbContext : DbContextBase
  {
    // Register the UserManager and SignInManager classes.
    services.AddIdentityCore<IdentityUser>()
        .AddEntityFrameworkStores<TDbContext>();

    // Register the SsoManager class and ISsoProvider interface.
    services.AddScoped<ISSoProvider, GenericSSOProvider>();
    services.AddScoped<GenericSSOManager>();
  }
}
