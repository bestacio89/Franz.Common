using Microsoft.AspNetCore.Builder;

namespace Franz.Common.Mediator.AspNetCore
{
  public static class MediatorContextMiddlewareExtensions
  {
    public static IApplicationBuilder UseMediatorContext(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<MediatorContextMiddleware>();
    }
  }
}
