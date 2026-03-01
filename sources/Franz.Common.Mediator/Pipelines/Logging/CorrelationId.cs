using System;
using System.Threading;

namespace Franz.Common.Mediator.Pipelines.Logging;

public static class CorrelationId
{
  private static readonly AsyncLocal<Guid?> _current = new();

  /// <summary>
  /// Gets or sets the current CorrelationId for the asynchronous flow.
  /// Defaults to Guid.Empty if not set.
  /// </summary>
  public static Guid Current
  {
    get => _current.Value ?? Guid.Empty;
    set => _current.Value = value;
  }

  /// <summary>
  /// Checks if a CorrelationId has been assigned to the current context.
  /// </summary>
  public static bool IsAssigned => _current.Value.HasValue && _current.Value != Guid.Empty;

  /// <summary>
  /// Ensures a CorrelationId exists by creating a new Guid v7 if the current context is empty.
  /// </summary>
  public static Guid Ensure()
  {
    if (!IsAssigned)
    {
      Current = Guid.CreateVersion7();
    }
    return Current;
  }
}