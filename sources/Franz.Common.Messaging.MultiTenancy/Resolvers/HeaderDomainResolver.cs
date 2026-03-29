#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy.Resolvers;

/// <summary>
/// Resolves the Domain ID from the "X-Domain-ID" message header.
/// Senior Note: Updated to support the serializable string[] contract and .NET 10 async standards.
/// </summary>
public sealed class HeaderDomainResolver(IMessageContextAccessor messageContextAccessor)
    : IDomainResolver
{
  private readonly IMessageContextAccessor _messageContextAccessor = messageContextAccessor;

  public int Order => 0;

  public Task<DomainResolutionResult> ResolveDomainAsync(object? context = null)
  {
    var headers = _messageContextAccessor.Current?.Message.Headers;

    // We use the first element [0] to ensure JSON wire-format consistency.
    if (headers != null &&
        headers.TryGetValue(HeaderConstants.DomainId, out var values) &&
        values.Length > 0 &&
        Guid.TryParse(values[0], out var domainId))
    {
      return Task.FromResult(
          DomainResolutionResult.Succeeded(domainId, "Resolved domain from message header.")
      );
    }

    return Task.FromResult(
        DomainResolutionResult.FailedResult("DomainId header not found in message.")
    );
  }
}