using Microsoft.Extensions.Hosting;

namespace Franz.Common.Extensions;

public static class HostEnvironmentExtensions
{
  public const string INTEGRATION = "integration";
  public const string VALIDATION = "recette";
  public const string PREPRODUCTION = "preprod";

  public static bool IsIntegration(this IHostEnvironment hostEnvironment)
  {
    return hostEnvironment.IsEnvironment(INTEGRATION);
  }

  public static bool IsValidation(this IHostEnvironment hostEnvironment)
  {
    return hostEnvironment.IsEnvironment(VALIDATION);
  }

  public static bool IsPreProduction(this IHostEnvironment hostEnvironment)
  {
    return hostEnvironment.IsEnvironment(PREPRODUCTION);
  }
}
