using Franz.Common.Mediator.Pipelines.Processors;
using Franz.Common.Mediator.Pipelines.Validation;
using Franz.Common.Mediator.Validation;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation
{
  public sealed class ValidationPreProcessor<TRequest> : IPreProcessor<TRequest>
  {
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPreProcessor(IEnumerable<IValidator<TRequest>> validators)
    {
      _validators = validators;
    }

    public async Task ProcessAsync(TRequest request, CancellationToken cancellationToken)
    {
      var errors = new List<ValidationError>();

      foreach (var validator in _validators)
      {
        var result = await validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid)
        {
          errors.AddRange(result.Errors);
        }
      }

      if (errors.Count > 0)
        throw new ValidationException(errors);
    }
  }
}
