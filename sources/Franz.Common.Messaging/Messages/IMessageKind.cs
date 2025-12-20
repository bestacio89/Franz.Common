using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Messages;

public interface IMessageKind
{
  MessageKind Kind { get; }
}