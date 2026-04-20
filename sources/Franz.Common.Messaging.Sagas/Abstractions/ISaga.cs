using Franz.Common.Messaging.Sagas.Abstractions;

public interface ISaga<TState>
    where TState : class, ISagaState, new()
{
  TState State { get; }
  Guid SagaId { get; }
  Task OnCreatedAsync(ISagaContext context, CancellationToken cancellationToken = default);
}