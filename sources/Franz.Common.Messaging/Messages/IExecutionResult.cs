using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Messages;


public interface IExecutionResult : ISystemMessage
{
  bool Success { get; }
  string? Message { get; }
}

