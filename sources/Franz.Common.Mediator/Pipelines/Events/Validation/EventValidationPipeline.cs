using France.Common.Extensions;
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
      var failures = new List<string>();

      foreach (var validator in _validators)
      {
        var result = await validator.ValidateAsync(@event, cancellationToken);
        if (result != null)
          failures.AddRange((IEnumerable<string>)result.Errors);
      }

      if (failures.Any())
      {
        var eventName = @event?.GetType().Name ?? typeof(TEvent).Name;

        if (_env.IsDevelopment())
        {
          _logger.LogWarning("[EventValidation] {EventName} failed with errors: {@Errors}",
              eventName, failures);
        }
        else
        {
          _logger.LogWarning("[EventValidation] {EventName} failed with {ErrorCount} errors",
              eventName, failures.Count);
        }

        throw new EventValidationException(failures);
      }

      if (_env.IsDevelopment())
      {
        var eventName = @event?.GetType().Name ?? typeof(TEvent).Name;
        _logger.LogInformation("[EventValidation] {EventName} passed", eventName);
      }

      await next();
    }
  }

  public class EventValidationException : Exception
  {
    public IEnumerable<string> Errors { get; }
    public EventValidationException(IEnumerable<string> errors)
        : base("Event validation failed")
    {
      Errors = errors;
    }
  }

