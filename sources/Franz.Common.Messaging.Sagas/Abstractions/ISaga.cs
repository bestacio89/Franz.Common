#nullable enable

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Defines a saga instance and its application-owned state.
/// </summary>
public interface ISaga<TState>
    where TState : class, ISagaState, new()
{
  TState State { get; }
  Guid SagaId { get; }

  /// <summary>
  /// Assigns the persisted identity when an existing saga instance is rehydrated.
  /// </summary>
  void SetId(Guid sagaId);

  Task OnCreatedAsync(ISagaContext context, CancellationToken cancellationToken = default);
}
