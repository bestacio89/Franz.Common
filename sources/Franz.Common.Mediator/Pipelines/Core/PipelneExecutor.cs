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
    if (pipelines.Count == 0)
      return handler(request, ct);

    var runner = new ValuePipelineRunner<TRequest, TResponse>(request, pipelines, handler, ct);
    return runner.Next();
  }

  public static Task ExecuteAsync<TRequest>(
      TRequest request,
      IReadOnlyList<IPipeline<TRequest, Unit>> pipelines,
      Func<TRequest, CancellationToken, Task> handler,
      CancellationToken ct = default)
      where TRequest : notnull
  {
    if (pipelines.Count == 0)
      return handler(request, ct);

    var runner = new VoidPipelineRunner<TRequest>(request, pipelines, handler, ct);
    return runner.NextVoid();
  }

  private sealed class ValuePipelineRunner<TRequest, TResponse> where TRequest : notnull
  {
    private readonly TRequest _request;
    private readonly IReadOnlyList<IPipeline<TRequest, TResponse>> _pipelines;
    private readonly Func<TRequest, CancellationToken, Task<TResponse>> _handler;
    private readonly CancellationToken _ct;
    private int _index;

    public ValuePipelineRunner(
        TRequest request,
        IReadOnlyList<IPipeline<TRequest, TResponse>> pipelines,
        Func<TRequest, CancellationToken, Task<TResponse>> handler,
        CancellationToken ct)
    {
      _request = request;
      _pipelines = pipelines;
      _handler = handler;
      _ct = ct;
      _index = 0;
    }

    public Task<TResponse> Next()
    {
      if ((uint)_index < (uint)_pipelines.Count)
      {
        var pipeline = _pipelines[_index++];
        return pipeline.Handle(_request, Next, _ct);
      }
      return _handler(_request, _ct);
    }
  }

  private sealed class VoidPipelineRunner<TRequest> where TRequest : notnull
  {
    private readonly TRequest _request;
    private readonly IReadOnlyList<IPipeline<TRequest, Unit>> _pipelines;
    private readonly Func<TRequest, CancellationToken, Task> _handler;
    private readonly CancellationToken _ct;
    private int _index;

    public VoidPipelineRunner(
        TRequest request,
        IReadOnlyList<IPipeline<TRequest, Unit>> pipelines,
        Func<TRequest, CancellationToken, Task> handler,
        CancellationToken ct)
    {
      _request = request;
      _pipelines = pipelines;
      _handler = handler;
      _ct = ct;
      _index = 0;
    }

    public async Task NextVoid()
    {
      await Next().ConfigureAwait(false);
    }

    private Task<Unit> Next()
    {
      if ((uint)_index < (uint)_pipelines.Count)
      {
        var pipeline = _pipelines[_index++];
        return pipeline.Handle(_request, Next, _ct);
      }

      return ExecuteTerminalAsync();
    }

    private async Task<Unit> ExecuteTerminalAsync()
    {
      await _handler(_request, _ct).ConfigureAwait(false);
      return Unit.Value;
    }
  }
}

