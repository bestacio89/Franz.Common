using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation
{

  public class ValidationPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipeline(IEnumerable<IValidator<TRequest>> validators)
    {
      _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
      var failures = new List<ValidationError>();

      foreach (var validator in _validators)
      {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid && result.Errors != null)
        {
          failures.AddRange(result.Errors);
        }
      }

      if (failures.Any())
        throw new ValidationException(failures);

      return await next();
    }
  }
}
