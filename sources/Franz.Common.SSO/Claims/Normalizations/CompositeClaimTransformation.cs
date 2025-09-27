using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Franz.Common.SSO.Claims.Normalization
{
  public class CompositeClaimsTransformation : IClaimsTransformation
  {
    private readonly IEnumerable<IClaimsNormalizer> _normalizers;

    public CompositeClaimsTransformation(IEnumerable<IClaimsNormalizer> normalizers)
        => _normalizers = normalizers;

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
      var p = principal;
      foreach (var n in _normalizers.Where(n => n.CanHandle(p)))
        p = n.Normalize(p);
      return Task.FromResult(p);
    }
  }
}
