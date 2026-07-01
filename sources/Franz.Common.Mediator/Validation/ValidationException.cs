using System;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Mediator.Validation;

public sealed class ValidationException : Exception
{
  public IReadOnlyList<ValidationError> Errors { get; }

  /// <summary>
  /// Optional context for classification (request, event, command, etc.)
  /// </summary>
  public string? Scope { get; }

  /// <summary>
  /// Stable error code for logging / telemetry aggregation.
  /// </summary>
  public string Code => "validation.failed";

  public ValidationException(IEnumerable<ValidationError> errors)
      : this(errors, scope: null)
  {
  }

  public ValidationException(IEnumerable<ValidationError> errors, string? scope)
      : base(BuildMessage(errors))
  {
    var materialized = errors?.ToList() ?? [];

    if (materialized.Count == 0)
      throw new ArgumentException("ValidationException requires at least one error.", nameof(errors));

    Errors = materialized.AsReadOnly();
    Scope = scope;
  }

  private static string BuildMessage(IEnumerable<ValidationError> errors)
  {
    // Avoid repeated enumeration
    var list = errors?.ToList() ?? [];

    return list.Count == 0
        ? "Validation failed."
        : "Validation failed: " + string.Join("; ", list.Select(e => e.ToString()));
  }

  public override string ToString()
  {
    return $"{GetType().Name} (Scope: {Scope ?? "request"}) - {Message}";
  }
}