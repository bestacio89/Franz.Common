using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Http.Refit.Contracts;
public interface ITokenProvider
{
  Task<string?> GetTokenAsync(CancellationToken ct = default);
}
