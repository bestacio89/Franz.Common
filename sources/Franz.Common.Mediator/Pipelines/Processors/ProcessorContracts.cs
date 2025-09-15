namespace Franz.Common.Mediator.Pipelines.Processors
{
  public interface IPreProcessor<in TRequest>
  {
    Task ProcessAsync(TRequest request, CancellationToken cancellationToken = default);
  }

  public interface IPostProcessor<in TRequest, in TResponse>
  {
    Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken = default);
  }
}
