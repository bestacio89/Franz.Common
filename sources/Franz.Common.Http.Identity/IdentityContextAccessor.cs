using Franz.Common.Headers;
using Franz.Common.Http.Extensions;
using Franz.Common.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Franz.Common.Http.Identity;

public class IdentityContextAccessor : IIdentityContextAccessor
{
  private readonly IHttpContextAccessor httpContextAccessor;

  public IdentityContextAccessor(IHttpContextAccessor httpContextAccessor)
  {
    this.httpContextAccessor = httpContextAccessor;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public string? GetCurrentEmail()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.UserEmail, ClaimTypes.Email);

    return result;
  }

  public Guid? GetCurrentId()
  {
    var userId = httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.UserId, ClaimTypes.NameIdentifier);

    Guid? result = null;
    if (Guid.TryParse(userId, out var guid))
      result = guid;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public string? GetCurrentFullName()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = httpContextAccessor.HttpContext.TryGetValue(HeaderConstants.UserFullName, ClaimTypes.Name);

    return result;
  }
}
