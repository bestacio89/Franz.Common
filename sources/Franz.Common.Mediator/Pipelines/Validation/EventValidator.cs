using Franz.Common.Mediator.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation;
public sealed class EventValidationException : Exception
{
  public IReadOnlyCollection<ValidationError> Errors { get; }

  public EventValidationException(IEnumerable<ValidationError> errors)
      : base("Event validation failed")
  {
    Errors = errors.ToList().AsReadOnly();
  }
}
