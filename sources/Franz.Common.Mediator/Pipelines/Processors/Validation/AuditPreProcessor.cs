using Franz.Common.Mediator.Pipelines.Processors;
using Franz.Common.Mediator.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation
{
  public sealed class AuditPreProcessor<TRequest> : IPreProcessor<TRequest>
  {
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<AuditPreProcessor<TRequest>> _logger;
    private readonly IHostEnvironment _env;

    public AuditPreProcessor(
      IEnumerable<IValidator<TRequest>> validators,
      ILogger<AuditPreProcessor<TRequest>> logger,
      IHostEnvironment env)
    {
      _validators = validators;
      _logger = logger;
      _env = env;
    }

    public async Task ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
    {
      var errors = new List<ValidationError>();

      foreach (var validator in _validators)
      {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid && result.Errors != null)
        {
          errors.AddRange(result.Errors);
        }
      }

      if (errors.Count > 0)
      {
        var requestType = request?.GetType().Name ?? typeof(TRequest).Name;

        if (_env.IsDevelopment())
        {
          // 🔥 Dev: log all errors
          _logger.LogWarning("[Pre-Validation] {RequestName} failed with errors: {@Errors}",
              requestType, errors);
        }
        else
        {
          // 🟢 Prod: only log count
          _logger.LogWarning("[Pre-Validation] {RequestName} failed with {ErrorCount} errors",
              requestType, errors.Count);
        }

        throw new ValidationException(errors);
      }

      if (_env.IsDevelopment())
      {
        var requestType = request?.GetType().Name ?? typeof(TRequest).Name;
        _logger.LogInformation("[Pre-Validation] {RequestName} passed", requestType);
      }
    }
  }
}
