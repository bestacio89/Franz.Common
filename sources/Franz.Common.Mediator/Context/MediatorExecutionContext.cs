using System;
using System.Globalization;

namespace Franz.Common.Mediator.Context;

/// <summary>
/// Immutable execution context for Mediator requests and events.
/// Carries correlation, causation, user, tenant and culture metadata.
/// </summary>
public sealed record MediatorExecutionContext
{
  public Guid CorrelationId { get; init; } = Guid.CreateVersion7();
  public Guid? CausationId { get; init; }
  public string? UserId { get; init; }
  public string? TenantId { get; init; }
  public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;
  public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
  public string? TraceIdentifier { get; init; }

  public static MediatorExecutionContext Empty => new()
  {
    CorrelationId = Guid.Empty
  };

  public MediatorExecutionContext WithCorrelationId(Guid correlationId)
      => this with { CorrelationId = correlationId };

  public MediatorExecutionContext WithCausationId(Guid? causationId)
      => this with { CausationId = causationId };

  public MediatorExecutionContext WithUser(string? userId)
      => this with { UserId = userId };

  public MediatorExecutionContext WithTenant(string? tenantId)
      => this with { TenantId = tenantId };

  public MediatorExecutionContext WithCulture(CultureInfo culture)
      => this with { Culture = culture ?? CultureInfo.CurrentCulture };
}
