using Franz.Common.Mediator.Dispatchers;

namespace Franz.Common.Mediator.Pipelines.Core;

public static class PipelineExecutor
{
  public static Task<TResponse> ExecuteAsync<TRequest, TResponse>(
      TRequest request,
      IReadOnlyList<IPipeline<TRequest, TResponse>> pipelines,
      Func<TRequest, CancellationToken, Task<TResponse>> handler,
      CancellationToken ct = default)
      where TRequest : notnull
  {
    ArgumentNullException.ThrowIfNull(pipelines);
    ArgumentNullException.ThrowIfNull(handler);

    if (pipelines.Count == 0)
      return handler(request, ct);

    return InvokeAt(0);

    Task<TResponse> InvokeAt(int index)
    {
      if (index >= pipelines.Count)
        return handler(request, ct);

      var pipeline = pipelines[index];
      return pipeline.Handle(request, () => InvokeAt(index + 1), ct);
    }
  }

  public static Task ExecuteAsync<TRequest>(
      TRequest request,
      IReadOnlyList<IPipeline<TRequest, Unit>> pipelines,
      Func<TRequest, CancellationToken, Task> handler,
      CancellationToken ct = default)
      where TRequest : notnull
  {
    ArgumentNullException.ThrowIfNull(pipelines);
    ArgumentNullException.ThrowIfNull(handler);

    if (pipelines.Count == 0)
      return handler(request, ct);

    return ExecuteCoreAsync();

    async Task ExecuteCoreAsync()
    {
      await InvokeAt(0).ConfigureAwait(false);
    }

    Task<Unit> InvokeAt(int index)
    {
      if (index >= pipelines.Count)
        return ExecuteTerminalAsync();

      var pipeline = pipelines[index];
      return pipeline.Handle(request, () => InvokeAt(index + 1), ct);
    }

    async Task<Unit> ExecuteTerminalAsync()
    {
      await handler(request, ct).ConfigureAwait(false);
      return Unit.Value;
    }
  }
}
