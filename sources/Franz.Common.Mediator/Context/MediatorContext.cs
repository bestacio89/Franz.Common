using System;
using System.Globalization;
using System.Threading;

namespace Franz.Common.Mediator.Context;

/// <summary>
/// Ambient context accessor (AsyncLocal backed).
/// </summary>
public static class MediatorContext
{
  private static readonly AsyncLocal<MediatorExecutionContext?> _current = new();

  public static MediatorExecutionContext Current
  {
    get => _current.Value ?? MediatorExecutionContext.Empty;
    private set => _current.Value = value;
  }

  public static Guid CorrelationId => Current.CorrelationId;
  public static string? UserId => Current.UserId;
  public static string? TenantId => Current.TenantId;
  public static CultureInfo Culture => Current.Culture;

  /// <summary>
  /// Set context for current async flow.
  /// </summary>
  public static void Set(MediatorExecutionContext context)
  {
    Current = context ?? MediatorExecutionContext.Empty;
  }

  /// <summary>
  /// Reset context (call in middleware finally block).
  /// </summary>
  public static void Reset()
  {
    _current.Value = null;
  }

  /// <summary>
  /// Ensure a correlation ID exists (creates Guid v7 if missing).
  /// </summary>
  public static Guid EnsureCorrelationId()
  {
    if (Current.CorrelationId == Guid.Empty)
    {
      Set(Current.WithCorrelationId(Guid.CreateVersion7()));
    }
    return Current.CorrelationId;
  }
}