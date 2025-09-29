using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using global::Franz.Common.Mediator.Pipelines.Events.Preprocessing;

namespace Franz.Common.Mediator.Validation.Events.Preprocessing;

/// <summary>
/// Pre-processor for validating events before they are dispatched.
/// Mirrors the AuditPreProcessor for requests.
/// </summary>
public sealed class EventAuditPreProcessor<TEvent> : IEventPreProcessor<TEvent>
  where TEvent : IEvent
{
  private readonly IEnumerable<IEventValidator<TEvent>> _validators;
  private readonly ILogger<EventAuditPreProcessor<TEvent>> _logger;
  private readonly IHostEnvironment _env;

  public EventAuditPreProcessor(
    IEnumerable<IEventValidator<TEvent>> validators,
    ILogger<EventAuditPreProcessor<TEvent>> logger,
    IHostEnvironment env)
  {
    _validators = validators;
    _logger = logger;
    _env = env;
  }

  public async Task ProcessAsync(TEvent @event, CancellationToken cancellationToken = default)
  {
    var allErrors = new List<ValidationError>();

    foreach (var validator in _validators)
    {
      var result = await validator.ValidateAsync(@event, cancellationToken);
      if (!result.IsValid)
      {
        allErrors.AddRange(result.Errors);
      }
    }

    var finalResult = new ValidationResult(allErrors);

    if (!finalResult.IsValid)
    {
      var eventType = @event?.GetType().Name ?? typeof(TEvent).Name;

      if (_env.IsDevelopment())
      {
        _logger.LogWarning("[Event-Validation] {EventName} failed with errors: {@Errors}",
            eventType, finalResult.Errors);
      }
      else
      {
        _logger.LogWarning("[Event-Validation] {EventName} failed with {ErrorCount} errors",
            eventType, finalResult.Errors.Count);
      }

      throw new EventValidationException(finalResult);
    }

    if (_env.IsDevelopment())
    {
      var eventType = @event?.GetType().Name ?? typeof(TEvent).Name;
      _logger.LogInformation("[Event-Validation] {EventName} passed", eventType);
    }
  }

}

