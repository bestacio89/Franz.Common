using Franz.Common.Business.Domain;

namespace Franz.Common.Business.AotSmokeTest;

public sealed class AotEntity : Entity<Guid>
{
  protected AotEntity(Guid id) : base(id) { }
}