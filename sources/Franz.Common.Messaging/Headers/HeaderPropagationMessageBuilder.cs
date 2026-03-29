#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Headers;

/// <summary>
/// Propagates ambient headers from the current context into the outgoing message.
/// Senior Note: Sealed for performance. Uses Primary Constructor for clean DI.
/// </summary>
public sealed class HeaderPropagationMessageBuilder(
    IHeaderContextAccessor headerContextAccessor,
    IHeaderPropagationRegistrer? headerPropagationRegistrer = null,
    HeaderPropagationOptions? headerPropagationOptions = null) : IMessageBuilder
{
  public bool CanBuild(Message message)
  {
    return
      (headerPropagationOptions?.Headers?.Any() == true) ||
      (headerPropagationRegistrer?.Headers?.Any() == true);
  }

  public Task BuildAsync(Message message, CancellationToken ct = default)
  {
    if (message.Headers is null)
      return Task.CompletedTask;

    // 1. Explicit headers from Options
    if (headerPropagationOptions?.Headers is not null)
    {
      foreach (var headerName in headerPropagationOptions.Headers)
      {
        PropagateHeader(message, headerName);
      }
    }

    // 2. Registered headers from Registrer
    if (headerPropagationRegistrer?.Headers is not null)
    {
      foreach (var registration in headerPropagationRegistrer.Headers)
      {
        PropagateHeader(message, registration.HeaderName);
      }
    }

    return Task.CompletedTask;
  }

  private void PropagateHeader(Message message, string headerName)
  {
    if (ShouldSkip(message, headerName))
      return;

    // Senior Note: We check the ContextAccessor for ambient values (TenantId, CorrelationId, etc.)
    if (headerContextAccessor.TryGetValue(headerName, out StringValues value))
    {
      message.Headers![headerName] = value;
    }
  }

  private static bool ShouldSkip(Message message, string headerName)
  {
    // Senior Note: Never override Franz invariants or headers already set by the domain
    return
      headerName.Equals("message-id", StringComparison.OrdinalIgnoreCase) ||
      headerName.Equals("correlation-id", StringComparison.OrdinalIgnoreCase) ||
      headerName.Equals("message-type", StringComparison.OrdinalIgnoreCase) ||
      message.Headers!.ContainsKey(headerName);
  }
}