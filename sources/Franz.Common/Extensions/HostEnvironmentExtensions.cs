namespace Microsoft.Extensions.Hosting;

public static class HostEnvironmentExtensions
{
  public const string INTEGRATION = "integration";
  public const string VALIDATION = "recette";
  public const string PREPRODUCTION = "preprod";

  public static bool IsIntegration(this IHostEnvironment hostEnvironment)
  {
    var result = hostEnvironment.IsEnvironment(INTEGRATION);

    return result;
  }

  public static bool IsValidation(this IHostEnvironment hostEnvironment)
  {
    var result = hostEnvironment.IsEnvironment(VALIDATION);

    return result;
  }

  public static bool IsPreProduction(this IHostEnvironment hostEnvironment)
  {
    var result = hostEnvironment.IsEnvironment(PREPRODUCTION);

    return result;
  }
}