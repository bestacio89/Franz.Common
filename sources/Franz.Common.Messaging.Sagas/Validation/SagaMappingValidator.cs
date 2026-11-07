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
/// to ensure handler signatures and identity-resolution constraints are respected.
/// </summary>
public static class SagaMappingValidator
{
  public static void ValidateRegistration(SagaRegistration reg)
  {
    SagaTypeValidator.ValidateSagaType(reg.SagaType);

    foreach (var start in reg.StartHandlers)
      ValidateHandler(reg, start.Key, start.Value);

    foreach (var step in reg.StepHandlers)
      ValidateHandler(reg, step.Key, step.Value);

    foreach (var comp in reg.CompensationHandlers)
      ValidateHandler(reg, comp.Key, comp.Value);

    ValidateSagaIdentityRules(reg);
  }

  private static void ValidateHandler(
      SagaRegistration reg,
      Type messageType,
      MethodInfo handler)
  {
    var parameters = handler.GetParameters();

    if (parameters.Length != 3)
      throw new SagaConfigurationError(
          HandlerError(reg, handler, "must have exactly 3 parameters."));

    if (parameters[0].ParameterType != messageType)
      throw new SagaConfigurationError(
          HandlerError(reg, handler, "first parameter must be the message type."));

    if (parameters[1].ParameterType != typeof(ISagaContext))
      throw new SagaConfigurationError(
          HandlerError(reg, handler, "second parameter must be ISagaContext."));

    if (parameters[2].ParameterType != typeof(System.Threading.CancellationToken))
      throw new SagaConfigurationError(
          HandlerError(reg, handler, "third parameter must be CancellationToken."));

    var returnType = handler.ReturnType;

    if (returnType == typeof(Task))
      return;

    if (returnType.IsGenericType &&
        returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
        returnType.GetGenericArguments()[0] == typeof(ISagaTransition))
      return;

    throw new SagaConfigurationError(
        HandlerError(reg, handler,
            "must return Task or Task<ISagaTransition>."));
  }

  private static void ValidateSagaIdentityRules(SagaRegistration reg)
  {
    var startTypes = reg.StartHandlers.Keys.ToHashSet();

    foreach (var messageType in reg.AllMessageTypes)
    {
      if (startTypes.Contains(messageType))
        continue;

      var resolverInterface = typeof(ISagaIdResolver<>).MakeGenericType(messageType);

      if (!reg.SagaType.GetInterfaces().Contains(resolverInterface))
      {
        throw new SagaConfigurationError(
            $"Saga '{reg.SagaType.Name}' does not implement " +
            $"ISagaIdResolver<{messageType.Name}> required to resolve the saga instance " +
            $"for non-start message '{messageType.Name}'.");
      }
    }
  }

  private static string HandlerError(SagaRegistration reg, MethodInfo method, string message)
      => $"Saga '{reg.SagaType.Name}' handler '{method.Name}' {message}";
}
