using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Dispatchers;
public class DispatchingStrategies
{
  public enum PublishStrategy
  {
    Sequential,
    Parallel
  }

  public enum NotificationErrorHandling
  {
    StopOnFirstFailure,
    ContinueOnError
  }
}
