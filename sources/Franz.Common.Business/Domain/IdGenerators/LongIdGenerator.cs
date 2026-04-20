using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.IdGenerators;

public sealed class LongIdGenerator : IIdGenerator<long>
{
  private long _current;

  public long Create() => Interlocked.Increment(ref _current);
}