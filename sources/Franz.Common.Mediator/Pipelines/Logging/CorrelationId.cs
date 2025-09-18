using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Logging;
public static class CorrelationId
{
  private static readonly AsyncLocal<string> _current = new();
  public static string? Current
  {
    get => _current.Value;
    set => _current.Value = value;
  }
}
