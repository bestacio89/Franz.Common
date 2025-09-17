using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Messages;
public interface IEvent : INotification
{
  DateTime OccurredOn { get; }
}