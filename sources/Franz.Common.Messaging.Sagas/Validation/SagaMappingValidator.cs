#nullable enable

using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Exceptions;
using System;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Messaging.Sagas.Validation;

/// <summary>
/// Validates saga message mappings (start handlers, steps, compensation steps)
/// to ensure handler signatures and correlation constraints are respected.
/// </summary>
public static class SagaMappingValidator
{
  public static void ValidateRegistration(SagaRegistration reg)
  {
    // Validate saga type first
    SagaTypeValidator.ValidateSagaType(reg.SagaType);

    foreach (var start in reg.StartHandlers)
      ValidateHandler(reg, start.Key, start.Value, isStart: true);

    foreach (var step in reg.StepHandlers)
      ValidateHandler(reg, step.Key, step.Value, isStart: false);

    foreach (var comp in reg.CompensationHandlers)
      ValidateHandler(reg, comp.Key, comp.Value, isStart: false);

    // Validate that non-start messages have correlation rules
    ValidateCorrelationRules(reg);
  }

  private static void ValidateHandler(
      SagaRegistration reg,
      Type messageType,
      MethodInfo handler,
      bool isStart)
  {
    var parameters = handler.GetParameters();

    // expected signature:
    // (TMessage message, ISagaContext ctx, CancellationToken ct)
    if (parameters.Length != 3)
      throw new SagaConfigurationException(
          HandlerError(reg, handler, "must have exactly 3 parameters."));

    if (parameters[0].ParameterType != messageType)
      throw new SagaConfigurationException(
          HandlerError(reg, handler, "first parameter must be the message type."));

    if (parameters[1].ParameterType != typeof(ISagaContext))
      throw new SagaConfigurationException(
          HandlerError(reg, handler, "second parameter must be ISagaContext."));

    if (parameters[2].ParameterType != typeof(System.Threading.CancellationToken))
      throw new SagaConfigurationException(
          HandlerError(reg, handler, "third parameter must be CancellationToken."));

    // Return type can be:
    // Task<ISagaTransition> or Task
    var returnType = handler.ReturnType;

    if (returnType == typeof(Task))
      return; // valid: no transition returned

    if (returnType.IsGenericType &&
        returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
        returnType.GetGenericArguments()[0] == typeof(ISagaTransition))
      return; // valid

    throw new SagaConfigurationException(
        HandlerError(reg, handler,
            "must return Task or Task<ISagaTransition>."));
  }

  private static void ValidateCorrelationRules(SagaRegistration reg)
  {
    var sagaType = reg.SagaType;

    // Start messages don't need correlation
    var startTypes = reg.StartHandlers.Keys.ToHashSet();

    // Other messages must implement IMessageCorrelation<T>
    foreach (var msgType in reg.AllMessageTypes)
    {
      if (startTypes.Contains(msgType))
        continue;

      // correlation interface:
      // IMessageCorrelation<TMessage>
      var correlationInterface = typeof(IMessageCorrelation<>).MakeGenericType(msgType);

      if (!sagaType.GetInterfaces().Contains(correlationInterface))
      {
        throw new SagaConfigurationException(
            $"Saga '{sagaType.Name}' does not implement IMessageCorrelation<{msgType.Name}> " +
            $"required for non-start message '{msgType.Name}'.");
      }
    }
  }

  private static string HandlerError(SagaRegistration reg, MethodInfo method, string message)
      => $"Saga '{reg.SagaType.Name}' handler '{method.Name}' {message}";
}
