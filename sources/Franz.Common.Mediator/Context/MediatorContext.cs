using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Franz.Common.Mediator.Context
{
  /// <summary>
  /// Holds contextual information for a mediator request lifecycle.
  /// Backed by AsyncLocal to flow across async calls automatically.
  /// </summary>
  public sealed class MediatorContext
  {
    private static readonly AsyncLocal<MediatorContext> _current = new();

    /// <summary>
    /// Current request context (per async flow).
    /// </summary>
    public static MediatorContext Current
    {
      get => _current.Value ??= new MediatorContext();
      internal set => _current.Value = value;
    }

    /// <summary>
    /// Reset context for a new request.
    /// </summary>
    public static void Reset() => Current = new MediatorContext();

    /// <summary>
    /// Unique correlation identifier for tracing.
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Current user id (optional, for auth/audit).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Current tenant id (optional, for multi-tenancy).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Current culture (for localization).
    /// </summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Arbitrary metadata bag for request-scoped values.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>();

    private MediatorContext() { }
  }
}
