#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Base saga that assigns a sortable, globally unique identity when the instance is created.
/// </summary>
public abstract class SagaBase<TState> : ISaga<TState>
    where TState : class, ISagaState, new()
{
  public Guid SagaId { get; private set; }

  public TState State { get; protected set; } = new();

  protected SagaBase()
  {
    SagaId = Guid.CreateVersion7();
  }

  /// <summary>
  /// Replaces the generated identity when an application explicitly supplies one.
  /// </summary>
  public void SetId(Guid id)
  {
    if (id == Guid.Empty)
      throw new ArgumentException("A saga identity cannot be empty.", nameof(id));

    SagaId = id;
  }

  public virtual Task OnCreatedAsync(ISagaContext context, CancellationToken ct = default)
      => Task.CompletedTask;
}
