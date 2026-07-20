using System;
using System.Globalization;

namespace Franz.Common.Mediator.Context;

/// <summary>
/// Immutable execution context for Mediator requests/events.
/// Carries correlation, user, tenant and other cross-cutting data.
/// </summary>
public sealed record MediatorExecutionContext
{
  public Guid CorrelationId { get; init; } = Guid.CreateVersion7();
  public string? UserId { get; init; }
  public string? TenantId { get; init; }
  public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;
  public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

  // Optional: original TraceIdentifier from HTTP
  public string? TraceIdentifier { get; init; }

  public static MediatorExecutionContext Empty => new();

  public MediatorExecutionContext WithCorrelationId(Guid correlationId) =>
      this with { CorrelationId = correlationId };

  public MediatorExecutionContext WithUser(string? userId) =>
      this with { UserId = userId };

  public MediatorExecutionContext WithTenant(string? tenantId) =>
      this with { TenantId = tenantId };

  public MediatorExecutionContext WithCulture(CultureInfo culture) =>
      this with { Culture = culture ?? CultureInfo.CurrentCulture };
}