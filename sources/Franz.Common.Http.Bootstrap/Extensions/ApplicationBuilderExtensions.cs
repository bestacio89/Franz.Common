namespace Microsoft.AspNetCore.Builder;

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