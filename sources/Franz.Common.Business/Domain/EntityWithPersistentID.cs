using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain;

public abstract class EntityWithPersistentId<TId> : Entity<TId>
{
  public Guid PersistentId { get; protected set; } = Guid.CreateVersion7();
}