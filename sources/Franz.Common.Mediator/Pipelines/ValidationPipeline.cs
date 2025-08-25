using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines
{
  public interface IValidator<in TRequest>
  {
    Task<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
  }

  public class ValidationPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
  {
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipeline(IEnumerable<IValidator<TRequest>> validators)
    {
      _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
      var failures = new List<string>();

      foreach (var validator in _validators)
      {
        var result = await validator.ValidateAsync(request, cancellationToken);
        failures.AddRange(result);
      }

      if (failures.Any())
      {
        throw new ValidationException(failures);
      }

      return await next();
    }
  }

  public class ValidationException : Exception
  {
    public IEnumerable<string> Errors { get; }
    public ValidationException(IEnumerable<string> errors)
        : base("Validation failed")
    {
      Errors = errors;
    }
  }
}
