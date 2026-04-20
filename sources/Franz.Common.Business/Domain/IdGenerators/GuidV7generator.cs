using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.IdGenerators;

public sealed class GuidV7Generator : IIdGenerator<Guid>
{
  public Guid Create() => Guid.CreateVersion7();
}