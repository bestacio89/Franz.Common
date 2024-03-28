using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Franz.Common.SSO.Interfaces;

namespace Franz.Common.SSO;
internal class GenericSSOProvider : ISSoProvider
{
  private readonly IConfiguration _configuration;

  public GenericSSOProvider(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public IdentityUser GetUser(string email)
  {

    return new IdentityUser { UserName = email, Email = email };
  }
}
