using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Validation;
public interface INotificationValidator<in TNotification>
{
  Task<IEnumerable<string>> ValidateAsync(
      TNotification notification,
      CancellationToken cancellationToken = default);
}