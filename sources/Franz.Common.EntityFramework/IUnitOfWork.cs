using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework;

public interface IUnitOfWork
{
  Task<int> CommitAsync(CancellationToken cancellationToken = default);
}