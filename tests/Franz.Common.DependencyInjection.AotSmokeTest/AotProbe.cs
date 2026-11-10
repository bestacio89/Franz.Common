namespace Franz.Common.DependencyInjection.AotSmokeTest;

public sealed class AotProbe : IAotProbe
{
  public string GetValue() => "explicit-registration";
}