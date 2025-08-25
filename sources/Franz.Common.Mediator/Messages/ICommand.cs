using Franz.Common.Mediator.Dispatchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Messages;


// A command that doesn't return a value
public interface ICommand : IMessage { }

// A command that returns a value of type TResponse
public interface ICommand<TResponse> : IMessage { }

// New interface that explicitly states commands without a response return Unit
public interface ICommandNoResponse : ICommand<Unit> { }