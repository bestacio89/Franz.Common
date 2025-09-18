using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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
    private readonly ILogger<ValidationPipeline<TRequest, TResponse>> _logger;
    private readonly IHostEnvironment _env;

    public ValidationPipeline(
      IEnumerable<IValidator<TRequest>> validators,
      ILogger<ValidationPipeline<TRequest, TResponse>> logger,
      IHostEnvironment env)
    {
      _validators = validators;
      _logger = logger;
      _env = env;
    }

    public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
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
      {
        var requestType = request?.GetType().Name ?? typeof(TRequest).Name;

        if (_env.IsDevelopment())
        {
          // 🔥 Dev: log full list of errors
          _logger.LogWarning("[Validation] {RequestName} failed with errors: {@Errors}",
              requestType, failures);
        }
        else
        {
          // 🟢 Prod: only count
          _logger.LogWarning("[Validation] {RequestName} failed with {ErrorCount} errors",
              requestType, failures.Count);
        }

        throw new ValidationException(failures);
      }

      if (_env.IsDevelopment())
      {
        _logger.LogInformation("[Validation] {RequestName} passed",
            request?.GetType().Name ?? typeof(TRequest).Name);
      }

      return await next();
    }
  }
}
