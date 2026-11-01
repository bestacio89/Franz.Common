using Franz.Common.Http;
using Microsoft.AspNetCore.Builder;
using Franz.Common.Http.Documentation.Extensions;
using Franz.Common.Http.MultiTenancy.Extensions;

namespace Franz.Common.Http.Bootstrap.Extensions;

public static class ApplicationBuilderExtensions
{
  /// <summary>
  /// Composes the standard Franz HTTP request pipeline.
  /// Tenant resolution runs after authentication (so JWT claims are available)
  /// and before authorization (so endpoint authorization executes with a validated
  /// tenant execution context).
  /// </summary>
  public static IApplicationBuilder UseHttpArchitecture(this IApplicationBuilder applicationBuilder)
  {
    ArgumentNullException.ThrowIfNull(applicationBuilder);

    applicationBuilder
      .UseExceptionHandler()
      .UseForwardedHeaders()
      .UseCertificateForwarding()
      .UseFrenchLocalization()
      .UseRouting()
      .UseDefaultCors()
      .UseAuthentication()
      .UseFranzMultiTenancy()
      .UseAuthorization()
      .UseEndpoints(endpoints =>
      {
        endpoints.MapControllers().RequireAuthorization();
      })
      .UseDocumentation()
      .UseHealthChecks("/health");

    return applicationBuilder;
  }
}
