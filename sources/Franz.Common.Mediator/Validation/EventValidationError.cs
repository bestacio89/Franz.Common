using Franz.Common.Mediator.Pipelines.Validation;
using System;
using System.Collections.Generic;
using System.Text;


namespace Franz.Common.Mediator.Validation;

public sealed class EventValidationException : Exception
{
  public ValidationResult Result { get; }

  public string Code => "event.validation.failed";

  public EventValidationException(ValidationResult result)
      : base("Event validation failed")
  {
    Result = result ?? throw new ArgumentNullException(nameof(result));

    if (!result.Errors.Any())
      throw new ArgumentException("EventValidationException requires at least one validation error.");
  }

  public override string ToString()
      => $"{Code}: {Message}";
}