using Franz.Common.Mediator.Pipelines.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Validation;
public sealed class EventValidationException : Exception
{
  public ValidationResult Result { get; }

  public EventValidationException(ValidationResult result)
      : base("Event validation failed")
  {
    Result = result;
  }
}
