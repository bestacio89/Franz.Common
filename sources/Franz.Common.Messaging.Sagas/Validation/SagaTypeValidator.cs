#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Exceptions;
using System;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Messaging.Sagas.Validation;

/// <summary>
/// Validates a saga type (class) independently of its message mappings.
/// Ensures that the saga follows the required Franz saga conventions.
/// </summary>
public static class SagaTypeValidator
{
  public static void ValidateSagaType(Type sagaType)
  {
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

    Type stateType = sagaInterface.GetGenericArguments()[0];

    // Validate state implements ISagaState
    if (!typeof(ISagaState).IsAssignableFrom(stateType))
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' has invalid state type '{stateType.Name}'. " +
          $"It must implement ISagaState.");

    // SagaId property
    var idProp = sagaType.GetProperty("SagaId");
    if (idProp == null || idProp.PropertyType != typeof(string))
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' must define a readable SagaId string property.");

    // State property
    var stateProp = sagaType.GetProperty("State");
    if (stateProp == null || !stateType.IsAssignableFrom(stateProp.PropertyType))
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' must define a State property of type '{stateType.Name}'.");

    // OnCreatedAsync method
    var createdMethod = sagaType.GetMethod("OnCreatedAsync");
    if (createdMethod == null)
      throw new SagaConfigurationError(
          $"Saga '{sagaType.Name}' must define an OnCreatedAsync(ISagaContext, CancellationToken) method.");
  }
}
