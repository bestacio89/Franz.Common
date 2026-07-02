using Franz.Common.Mediator.Validation;
using Franz.Common.Mediator.Pipelines.Validation;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation;

public sealed class NotNullValidator<TRequest> : IValidator<TRequest>
{
  public Task<ValidationResult> ValidateAsync(
      TRequest instance,
      CancellationToken cancellationToken)
  {
    var errors = new List<ValidationError>();

    if (instance is null)
    {
      errors.Add(new ValidationError(
        string.Empty,
        "Request cannot be null"));
    }

    return Task.FromResult(new ValidationResult(errors));
  }
}