using Franz.Common.Business.Domain.IdGenerators;

namespace Franz.Common.Business.AotSmokeTest;

internal sealed class AotGuidGenerator : IIdGenerator<Guid>
{
  public Guid Create() => Guid.NewGuid();
}