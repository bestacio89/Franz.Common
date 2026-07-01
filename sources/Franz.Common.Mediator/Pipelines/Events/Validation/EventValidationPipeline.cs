using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Validation.Events.Validation;

public class EventValidationPipeline<TEvent> : IEventPipeline<TEvent>
    where TEvent : IEvent
{
  private readonly IEnumerable<IEventValidator<TEvent>> _validators;
  private readonly ILogger<EventValidationPipeline<TEvent>> _logger;
  private readonly IHostEnvironment _env;

  public EventValidationPipeline(
      IEnumerable<IEventValidator<TEvent>> validators,
      ILogger<EventValidationPipeline<TEvent>> logger,
      IHostEnvironment env)
  {
    _validators = validators;
    _logger = logger;
    _env = env;
  }

  public async Task HandleAsync(
      TEvent @event,
      Func<Task> next,
      CancellationToken cancellationToken = default)
  {
    var allErrors = new List<ValidationError>();

    foreach (var validator in _validators)
    {
      ValidationResult result = await validator.ValidateAsync(@event, cancellationToken);

      if (result?.IsValid == true)
        continue;

      if (result?.Errors != null)
        allErrors.AddRange(result.Errors);
    }

    if (allErrors.Any())
    {
      var eventName = @event?.GetType().Name ?? typeof(TEvent).Name;

      if (_env.IsDevelopment())
      {
        _logger.LogWarning(
          "[EventValidation] {EventName} failed with errors: {@Errors}",
          eventName,
          allErrors);
      }
      else
      {
        _logger.LogWarning(
          "[EventValidation] {EventName} failed with {ErrorCount} errors",
          eventName,
          allErrors.Count);
      }

      var validationResult = new ValidationResult(allErrors);

      throw new EventValidationException(validationResult);
    }

    if (_env.IsDevelopment())
    {
      _logger.LogInformation(
        "[EventValidation] {EventName} passed",
        @event?.GetType().Name ?? typeof(TEvent).Name);
    }

    await next();
  }
}