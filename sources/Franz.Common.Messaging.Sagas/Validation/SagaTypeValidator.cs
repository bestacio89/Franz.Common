#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;

namespace Franz.Common.Messaging.Sagas.Validation;

/// <summary>
/// Validates saga types independently of their message mappings.
/// </summary>
public static class SagaTypeValidator
{
  public static void ValidateSagaType(Type sagaType)
  {
    ArgumentNullException.ThrowIfNull(sagaType);

    if (!sagaType.IsClass || sagaType.IsAbstract)
      throw new SagaConfigurationError(
          $"Saga type '{sagaType.Name}' must be a non-abstract class.");

    var sagaInterface = sagaType.GetInterfaces()
        .FirstOrDefault(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(ISaga<>));

    if (sagaInterface is null)
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' must implement ISaga<TState>.");

    var stateType = sagaInterface.GetGenericArguments()[0];
    if (!typeof(ISagaState).IsAssignableFrom(stateType))
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' has invalid state type '{stateType.Name}'. " +
          "It must implement ISagaState.");

    var idProperty = sagaType.GetProperty("SagaId");
    if (idProperty is null ||
        idProperty.PropertyType != typeof(Guid) ||
        idProperty.GetMethod is not { IsPublic: true })
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' must expose a readable Guid SagaId property.");

    var setIdMethod = sagaType.GetMethod("SetId", new[] { typeof(Guid) });
    if (setIdMethod is null || !setIdMethod.IsPublic || setIdMethod.ReturnType != typeof(void))
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' must expose public SetId(Guid) for persisted identity rehydration.");

    var stateProperty = sagaType.GetProperty("State");
    if (stateProperty is null || !stateType.IsAssignableFrom(stateProperty.PropertyType))
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' must define a State property of type '{stateType.Name}'.");

    var createdMethod = sagaType.GetMethod(
        "OnCreatedAsync",
        new[] { typeof(ISagaContext), typeof(CancellationToken) });

    if (createdMethod is null)
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' must define OnCreatedAsync(ISagaContext, CancellationToken).");
  }
}
