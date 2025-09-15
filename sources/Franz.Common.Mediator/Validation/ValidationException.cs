using System;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Mediator.Validation
{
  public sealed class ValidationException : Exception
  {
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(IEnumerable<ValidationError> errors)
        : base(BuildMessage(errors))
    {
      Errors = errors.ToList().AsReadOnly();
    }

    private static string BuildMessage(IEnumerable<ValidationError> errors) =>
        "Validation failed: " + string.Join("; ", errors.Select(e => e.ToString()));
  }
}
