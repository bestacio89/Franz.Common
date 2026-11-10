using Franz.Common.Business.Domain;

namespace Franz.Common.Business.AotSmokeTest;

public sealed class AotEnumeration : Enumeration<int>
{
  public static readonly AotEnumeration Ready = new(1, "Ready");
  public static readonly AotEnumeration Completed = new(2, "Completed");

  private AotEnumeration(int id, string name) : base(id, name) { }
}