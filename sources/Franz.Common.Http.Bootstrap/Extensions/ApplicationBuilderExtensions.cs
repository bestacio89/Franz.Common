using Franz.Common.Http;
using Microsoft.AspNetCore.Builder;
using Franz.Common.Http.Documentation.Extensions;

namespace Franz.Common.Http.Bootstrap.Extensions;

public static class ApplicationBuilderExtensions
{
  public static IApplicationBuilder UseHttpArchitecture(this IApplicationBuilder applicationBuilder)
  {
    applicationBuilder
      .UseCertificateForwarding()
      .UseFrenchLocalization()
      .UseRouting()
      .UseDefaultCors()
      .UseAuthentication()
      .UseAuthorization()
      .UseForwardedHeaders()
      .UseEndpoints(endpoints =>
      {
        endpoints.MapControllers().RequireAuthorization();
      })
      .UseDocumentation()
      .UseHealthChecks("/health");

    return applicationBuilder;
  }
}