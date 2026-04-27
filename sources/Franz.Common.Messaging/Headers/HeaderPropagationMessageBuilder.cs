#nullable enable

using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;
using System;
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
    HeaderPropagationOptions? headerPropagationOptions = null
) : IMessageBuilder
{
  public bool CanBuild(Message message)
  {
    return
        (headerPropagationOptions?.Headers?.Any() == true) ||
        (headerPropagationRegistrer?.Headers?.Any() == true);
  }

  public Task BuildAsync(Message message, CancellationToken ct = default)
  {
    var headers = message.Headers;

    if (headers is null)
      return Task.CompletedTask;

    // 1. Explicit headers from Options
    if (headerPropagationOptions?.Headers is { } optionHeaders)
    {
      foreach (var headerName in optionHeaders)
      {
        PropagateHeader(headers, message, headerName);
      }
    }

    // 2. Registered headers from Registrer
    if (headerPropagationRegistrer?.Headers is { } registeredHeaders)
    {
      foreach (var registration in registeredHeaders)
      {
        PropagateHeader(headers, message, registration.HeaderName);
      }
    }

    return Task.CompletedTask;
  }

  private void PropagateHeader(
      IDictionary<string, string[]> headers,
      Message message,
      string headerName)
  {
    if (ShouldSkip(headers, headerName))
      return;

    if (headerContextAccessor.TryGetValue(headerName, out StringValues value))
    {
      var sanitized = value
          .Where(v => v is not null)
          .Select(v => v!)
          .ToArray();

      if (sanitized.Length > 0)
      {
        headers[headerName] = sanitized;
      }
    }
  }

  private static bool ShouldSkip(
      IDictionary<string, string[]> headers,
      string headerName)
  {
    return
        headerName.Equals("message-id", StringComparison.OrdinalIgnoreCase) ||
        headerName.Equals("correlation-id", StringComparison.OrdinalIgnoreCase) ||
        headerName.Equals("message-type", StringComparison.OrdinalIgnoreCase) ||
        headers.ContainsKey(headerName);
  }
}