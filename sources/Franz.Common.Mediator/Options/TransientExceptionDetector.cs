using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Options;
public static class TransientExceptionDetector
{
  public static bool Default(Exception ex) =>
      ex is TimeoutException
   || ex is TaskCanceledException // usually a timeout; adjust if you want to skip true cancels
   || ex is HttpRequestException
   // add DB/driver-specific transient checks here (e.g., SQL/Npgsql/EF concurrency)
   || ex.GetType().Name.Contains("Transient", StringComparison.OrdinalIgnoreCase);
}
