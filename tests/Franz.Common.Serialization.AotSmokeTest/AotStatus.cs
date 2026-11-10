using Franz.Common.Business.Domain;

namespace Franz.Common.Serialization.AotSmokeTest;

public sealed class AotStatus : Enumeration<int>
{
  public static readonly AotStatus Ready = new(1, "Ready");
  public static readonly AotStatus Finished = new(2, "Finished");

  private AotStatus(int id, string name) : base(id, name) { }
}