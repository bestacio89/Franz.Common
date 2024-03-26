namespace Franz.Common.Business.Commands;

public interface ICommandRequest<out TResult> : IRequest<TResult>, ICommandBaseRequest { }

public interface ICommandRequest : IRequest<Unit>, ICommandRequest<Unit> { }
