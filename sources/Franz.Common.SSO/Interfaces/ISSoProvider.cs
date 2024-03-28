using Microsoft.AspNetCore.Identity;

namespace Franz.Common.SSO.Interfaces;
public interface ISSoProvider
{
   IdentityUser GetUser(string email);
}
