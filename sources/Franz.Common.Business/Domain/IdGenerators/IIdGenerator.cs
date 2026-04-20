using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.IdGenerators;

public interface IIdGenerator<TId>
{
  TId Create();
}
