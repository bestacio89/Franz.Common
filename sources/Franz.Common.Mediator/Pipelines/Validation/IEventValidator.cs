using Franz.Common.Mediator.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation;
public interface IEventValidator<TEvent>
{
  Task<ValidationResult> ValidateAsync(TEvent @event, CancellationToken cancellationToken = default);
}