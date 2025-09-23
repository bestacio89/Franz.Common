using System.Threading;

namespace Franz.Common.Mediator.Pipelines.Logging;

public static class CorrelationId
{
  private static readonly AsyncLocal<string?> _current = new();

  public static string? Current
  {
    get => _current.Value;
    set => _current.Value = value;
  }
}
