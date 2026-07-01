using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Errors;



public class BusinessException : Exception
{
  /// <summary>
  /// Stable, machine-readable error code (no HTTP semantics).
  /// Example: "item.name.duplicate"
  /// </summary>
  public string Code { get; }

  /// <summary>
  /// Optional structured metadata for debugging / tracing.
  /// Should remain safe for logs (NOT secrets).
  /// </summary>
  public IReadOnlyDictionary<string, object?> Metadata { get; }

  public BusinessException(
      string code,
      string message)
      : base(message)
  {
    Code = code;
    Metadata = new Dictionary<string, object?>();
  }

  public BusinessException(
      string code,
      string message,
      IDictionary<string, object?> metadata)
      : base(message)
  {
    Code = code;
    Metadata = new Dictionary<string, object?>(metadata);
  }

  public BusinessException(
      string code,
      string message,
      Exception innerException)
      : base(message, innerException)
  {
    Code = code;
    Metadata = new Dictionary<string, object?>();
  }
}
