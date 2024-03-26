namespace Franz.Common.Business.Queries;
public interface IQueryHandler<TRequest, TResult> : IRequestHandler<TRequest, TResult>
    where TRequest : IQueryRequest<TResult>
{
}
