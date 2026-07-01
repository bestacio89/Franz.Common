using Franz.Common.Mediator.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Mediator.Pipelines.Validation;

public sealed class EventValidationException : Exception
{
  public ValidationResult Result { get; }

  public EventValidationException(ValidationResult result)
      : base("Event validation failed")
  {
    Result = result ?? throw new ArgumentNullException(nameof(result));

    if (result.Errors.Count == 0)
      throw new ArgumentException(
        "EventValidationException requires at least one validation error.",
        nameof(result));
  }
}