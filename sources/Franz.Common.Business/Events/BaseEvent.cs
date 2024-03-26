using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Business.Events;
public  abstract class BaseEvent : IEvent
{
  public DateTimeOffset Date { get; set; }
}
