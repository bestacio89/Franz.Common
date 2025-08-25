using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Messages;
// A query that returns a value of type TResponse
public interface IQuery<TResponse> : IMessage { }

