using Franz.Common.Business.Domain;

namespace Franz.Common.Business.Tests.Samples;
public class EntitySample : Entity
{
  public EntitySample(int id)
  {
    Id = id;
  }
}

public class EntitySample<Guid> : Entity<Guid>
{
  public EntitySample(Guid id)
  {
    Id = id;
  }
}
