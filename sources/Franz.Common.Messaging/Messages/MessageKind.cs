using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Messages;

public enum MessageKind
{
  Command,
  Query,
  IntegrationEvent,
  Fault
}