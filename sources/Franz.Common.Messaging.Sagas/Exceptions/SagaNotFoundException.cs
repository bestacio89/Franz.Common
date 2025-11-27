using Franz.Common.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Sagas.Exceptions;

internal class SagaNotFoundException : FunctionalException
{
  public SagaNotFoundException(string message) : base(message)
  {
  }
}
