#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Identity;

/// <summary>
/// Enriches messaging headers with Authorization context from the current scope.
/// Senior Architect Note: Standardizing on Task-based enrichment for potential remote identity resolution.
/// </summary>
public class AuthorizationMessageBuilder : IMessageBuilder
{
  private readonly IHeaderContextAccessor? _headerContextAccessor;

  public AuthorizationMessageBuilder(IHeaderContextAccessor? headerContextAccessor = null)
  {
    _headerContextAccessor = headerContextAccessor;
  }

  /// <summary>
  /// Validates if the builder has the necessary context to enrich the message.
  /// </summary>
  public bool CanBuild(Message message)
  {
    return _headerContextAccessor is not null;
  }

  /// <summary>
  /// Asynchronously propagates the Authorization header to the message envelope.
  /// Senior Architect Note: Using Task.CompletedTask as this is currently a synchronous memory-copy operation.
  /// </summary>
  public Task BuildAsync(Message message, CancellationToken ct = default)
  {
    if (_headerContextAccessor is not null &&
        _headerContextAccessor.TryGetValue(HeaderConstants.Authorization, out StringValues values))
    {
      var array = values
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Select(v => v!) // remove nullable
        .ToArray();

      if (array.Length > 0)
      {
        message.Headers[HeaderConstants.Authorization] = array;
      }
    }

    return Task.CompletedTask;
  }
}