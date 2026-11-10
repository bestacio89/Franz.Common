#nullable enable

using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Transport-neutral metadata used to route a durable retry delivery to its saga instance.
/// The message identity is independently verified against the persisted retry schedule.
/// </summary>
public static class SagaRetryHeaders
{
  public const string TargetSagaId = "franz-saga-retry-target";

  public static Guid? ReadTargetSagaId(Message? message)
  {
    if (message is null ||
        !message.Headers.TryGetValue(TargetSagaId, out var values))
      return null;

    if (values is not { Length: 1 } ||
        !Guid.TryParse(values[0], out var sagaId) ||
        sagaId == Guid.Empty)
      throw new PermanentMessagingException(
          "Saga retry metadata must contain exactly one non-empty target saga GUID.");

    return sagaId;
  }
}