using Franz.Common.Mediator.Validation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation
{
  public sealed class FluentValidationAdapter<TRequest> : IValidator<TRequest>
  {
    private readonly FluentValidation.IValidator<TRequest> _inner;

    public FluentValidationAdapter(FluentValidation.IValidator<TRequest> inner)
    {
      _inner = inner;
    }

    public async Task<ValidationResult> ValidateAsync(TRequest instance, CancellationToken cancellationToken)
    {
      var result = await _inner.ValidateAsync(instance, cancellationToken);

      if (result.IsValid)
        return ValidationResult.Success();

      var errors = result.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage));
      return ValidationResult.Failure(errors);
    }
  }
}
