#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Base contract for all sagas.
/// A saga is a long-running, message-driven workflow with state.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public interface ISaga<TState>
  where TState : class, ISagaState, new()
{
  /// <summary>
  /// Gets the current saga state instance.
  /// </summary>
  TState State { get; }

  /// <summary>
  /// Gets the logical saga identifier used for correlation.
  /// This is typically derived from <see cref="State"/>.
  /// </summary>
  string SagaId { get; }

  /// <summary>
  /// Called by the orchestrator when the saga is first created.
  /// Implementations can initialize default values in <see cref="State"/>.
  /// </summary>
  Task OnCreatedAsync(ISagaContext context, CancellationToken cancellationToken = default);
}
