using Franz.Common.MultiTenancy;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;

namespace Franz.Common.Messaging.MultiTenancy.Resolvers;
#nullable enable
public class HeaderDomainResolver : IDomainResolver
{
  private readonly IMessageContextAccessor _messageContextAccessor;

  public HeaderDomainResolver(IMessageContextAccessor messageContextAccessor)
  {
    _messageContextAccessor = messageContextAccessor;
  }

  public int Order => 0;

  public Task<DomainResolutionResult> ResolveDomainAsync(object? context = null)
  {
    if (_messageContextAccessor.Current?.Message.Headers.TryGetDomainId(out var domainId) == true)
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
