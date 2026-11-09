#nullable enable

using Franz.Common.Headers;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.Sagas.Handlers;

/// <summary>
/// Resolves tenant scope only from the validated tenant execution context.
/// A raw message header is accepted only as a value to verify against that context.
/// </summary>
internal static class SagaTenantContextResolver
{
  public static string? ResolveTenantId(
      Message? message,
      ITenantExecutionContextAccessor? tenantExecutionContextAccessor)
  {
    var tenantContext = tenantExecutionContextAccessor?.Current;

    if (message is not null &&
        message.Headers.TryGetValue(HeaderConstants.TenantId, out var tenantValues))
    {
      if (tenantValues.Length != 1 ||
          !Guid.TryParse(tenantValues[0], out var headerTenantId) ||
          headerTenantId == Guid.Empty)
      {
        throw new PermanentMessagingException(
            "Saga message tenant metadata must contain exactly one non-empty GUID tenant identifier.");
      }

      if (tenantContext is null)
        throw new PermanentMessagingException(
            "Saga message contains tenant metadata but no validated tenant execution context is available.");

      if (tenantContext.TenantId != headerTenantId)
        throw new PermanentMessagingException(
            "Saga message tenant metadata does not match the validated tenant execution context.");
    }

    return tenantContext?.TenantId.ToString("D");
  }
}
