#nullable enable

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Execution pipeline for saga handlers. Middleware is configured before host startup
/// and remains immutable while messages are being processed.
/// </summary>
public sealed class SagaExecutionPipeline
{
  private readonly List<Func<Func<Task>, Task>> _middlewares = new();
  private bool _frozen;

  public SagaExecutionPipeline Use(Func<Func<Task>, Task> middleware)
  {
    if (_frozen)
      throw new InvalidOperationException("Saga execution pipeline is frozen after startup.");

    ArgumentNullException.ThrowIfNull(middleware);
    _middlewares.Add(middleware);
    return this;
  }

  internal void Freeze() => _frozen = true;

  public Task ExecuteAsync(Func<Task> final)
  {
    ArgumentNullException.ThrowIfNull(final);

    Task Handler(int index)
    {
      if (index == _middlewares.Count)
        return final();

      return _middlewares[index](() => Handler(index + 1));
    }

    return Handler(0);
  }
}
