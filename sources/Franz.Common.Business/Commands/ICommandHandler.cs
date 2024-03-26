
namespace Franz.Common.Business.Commands;

public interface ICommandHandler<TCommand, TResult> : IRequestHandler<TCommand, TResult>
  where TCommand : ICommandRequest<TResult>
{ }


