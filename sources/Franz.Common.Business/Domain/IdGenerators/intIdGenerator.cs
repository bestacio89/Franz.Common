using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.IdGenerators;

public sealed class IntIdGenerator : IIdGenerator<int>
{
  private int _current = 0;

  public int Create() => Interlocked.Increment(ref _current);
}