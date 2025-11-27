#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Messaging.Sagas.Abstractions;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Base class for saga implementations.
/// Provides state management, identity, and lifecycle hooks.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public abstract class SagaBase<TState> : ISaga<TState>
  where TState : class, ISagaState, new()
{
  /// <summary>
  /// The mutable state of the saga instance.
  /// </summary>
  public TState State { get; protected set; } = new();

  /// <summary>
  /// Gets the unique saga identifier.
  /// Override if saga ID is derived differently.
  /// </summary>
  public virtual string SagaId => State switch
  {
    ISagaStateWithId idState => idState.Id,
    _ => throw new SagaConfigurationException(
      $"Saga {GetType().Name} does not provide an ID. Implement ISagaStateWithId or override SagaId.")
  };

  /// <summary>
  /// Called when the saga is created for the first time.
  /// Override as needed.
  /// </summary>
  public virtual Task OnCreatedAsync(ISagaContext context, CancellationToken cancellationToken = default)
    => Task.CompletedTask;
}

/// <summary>
/// Optional interface for states that expose an ID.
/// </summary>
public interface ISagaStateWithId
{
  string Id { get; set; }
}

/// <summary>
/// Thrown when saga configuration violates Franz deterministic rules.
/// </summary>
public class SagaConfigurationException : Exception
{
  public SagaConfigurationException(string message) : base(message) { }
}
