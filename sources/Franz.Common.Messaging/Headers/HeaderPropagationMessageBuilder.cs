using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Headers;

public sealed class HeaderPropagationMessageBuilder : IMessageBuilder
{
  private readonly IHeaderContextAccessor _headerContextAccessor;
  private readonly IHeaderPropagationRegistrer? _headerPropagationRegistrer;
  private readonly HeaderPropagationOptions? _headerPropagationOptions;

  public HeaderPropagationMessageBuilder(
      IHeaderContextAccessor headerContextAccessor,
      IHeaderPropagationRegistrer? headerPropagationRegistrer = null,
      HeaderPropagationOptions? headerPropagationOptions = null)
  {
    _headerContextAccessor = headerContextAccessor;
    _headerPropagationRegistrer = headerPropagationRegistrer;
    _headerPropagationOptions = headerPropagationOptions;
  }

  public bool CanBuild(Message message)
  {
    return
      (_headerPropagationOptions?.Headers?.Any() == true) ||
      (_headerPropagationRegistrer?.Headers?.Any() == true);
  }

  public void Build(Message message)
  {
    if (message.Headers is null)
      return;

    // Explicit headers from options
    if (_headerPropagationOptions?.Headers is not null)
    {
      foreach (var headerName in _headerPropagationOptions.Headers)
      {
        if (ShouldSkip(message, headerName))
          continue;

        if (_headerContextAccessor.TryGetValue(headerName, out StringValues value))
        {
          message.Headers[headerName] = value;
        }
      }
    }

    // Registered headers
    if (_headerPropagationRegistrer?.Headers is not null)
    {
      foreach (var registration in _headerPropagationRegistrer.Headers)
      {
        var headerName = registration.HeaderName;

        if (ShouldSkip(message, headerName))
          continue;

        if (_headerContextAccessor.TryGetValue(headerName, out StringValues value))
        {
          message.Headers[headerName] = value;
        }
      }
    }
  }

  private static bool ShouldSkip(Message message, string headerName)
  {
    // Never override Franz invariants
    return
      headerName.Equals("message-id", StringComparison.OrdinalIgnoreCase) ||
      headerName.Equals("correlation-id", StringComparison.OrdinalIgnoreCase) ||
      headerName.Equals("message-type", StringComparison.OrdinalIgnoreCase) ||
      message.Headers.ContainsKey(headerName);
  }
}
