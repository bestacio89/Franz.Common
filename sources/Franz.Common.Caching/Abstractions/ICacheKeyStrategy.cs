using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Abstractions;
public interface ICacheKeyStrategy
{
  string GetKey(object request);
  public string BuildKey<TRequest>(TRequest request);
}

