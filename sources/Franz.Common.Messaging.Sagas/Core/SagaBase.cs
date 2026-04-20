#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Base saga with deterministic identity.
/// </summary>
public abstract class SagaBase<TState> : ISaga<TState>
    where TState : class, ISagaState, new()
{
  public Guid SagaId { get; private set; }

  public TState State { get; protected set; } = new();

  protected SagaBase()
  {
  }

  /// <summary>
  /// Factory-controlled initialization
  /// </summary>
  public void SetId(Guid id)
  {
    SagaId = id;
  }

  public virtual Task OnCreatedAsync(ISagaContext context, CancellationToken ct = default)
      => Task.CompletedTask;
}