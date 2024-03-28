using Microsoft.AspNetCore.Identity;
using Franz.Common.SSO.Interfaces;

namespace Franz.Common.SSO;
public class GenericSSOManager
{
  private readonly ISSoProvider _ssoProvider;
  private readonly UserManager<IdentityUser> _userManager;
  private readonly SignInManager<IdentityUser> _signInManager;

  public GenericSSOManager(ISSoProvider ssoProvider, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
  {
    _ssoProvider = ssoProvider;
    _userManager = userManager;
    _signInManager = signInManager;
  }

  public async Task<bool> Login(string email)
  {
    var user = _ssoProvider.GetUser(email);
    if (user == null)
    {
      return false;
    }

    var identityResult = await _signInManager.CheckPasswordSignInAsync(user, string.Empty, false);
    if (identityResult.Succeeded)
    {
      await _signInManager.SignInAsync(user, false);
      return true;
    }

    return false;
  }

  public async Task Logout()
  {
    await _signInManager.SignOutAsync();
  }
}
