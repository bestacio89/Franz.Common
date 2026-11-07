using System;
using System.Globalization;
using System.Threading;

namespace Franz.Common.Mediator.Context;

/// <summary>
/// Ambient context accessor backed by AsyncLocal.
/// </summary>
public static class MediatorContext
{
  private static readonly AsyncLocal<MediatorExecutionContext?> _current = new();

  public static MediatorExecutionContext Current
  {
    get => _current.Value ?? MediatorExecutionContext.Empty.WithCorrelationId(Guid.CreateVersion7());
    private set => _current.Value = value;
  }

  public static Guid CorrelationId => Current.CorrelationId;
  public static Guid? CausationId => Current.CausationId;
  public static string? UserId => Current.UserId;
  public static string? TenantId => Current.TenantId;
  public static CultureInfo Culture => Current.Culture;

  public static void Set(MediatorExecutionContext context)
      => _current.Value = context ?? MediatorExecutionContext.Empty;

  /// <summary>
  /// Establishes an ambient context for the current async flow and restores the previous
  /// value when the returned scope is disposed.
  /// </summary>
  public static IDisposable BeginScope(MediatorExecutionContext context)
  {
    ArgumentNullException.ThrowIfNull(context);
    var previous = _current.Value;
    _current.Value = context;
    return new ContextScope(previous);
  }

  public static void Reset() => _current.Value = null;

  public static Guid EnsureCorrelationId()
  {
    var context = _current.Value ?? MediatorExecutionContext.Empty;

    if (context.CorrelationId == Guid.Empty)
      context = context.WithCorrelationId(Guid.CreateVersion7());

    _current.Value = context;
    return context.CorrelationId;
  }

  private sealed class ContextScope(MediatorExecutionContext? previous) : IDisposable
  {
    private int _disposed;

    public void Dispose()
    {
      if (Interlocked.Exchange(ref _disposed, 1) == 0)
        _current.Value = previous;
    }
  }
}
