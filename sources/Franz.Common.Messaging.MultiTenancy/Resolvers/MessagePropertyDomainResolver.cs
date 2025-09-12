using Franz.Common.MultiTenancy;
using Franz.Common.Messaging.Contexting;

namespace Franz.Common.Messaging.MultiTenancy.Resolvers;

public class MessagePropertyDomainResolver : IDomainResolver
{
  private readonly IMessageContextAccessor _messageContextAccessor;

  public MessagePropertyDomainResolver(IMessageContextAccessor messageContextAccessor)
  {
    _messageContextAccessor = messageContextAccessor;
  }

  public int Order => 1;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public Task<DomainResolutionResult> ResolveDomainAsync(object? context = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var msg = _messageContextAccessor.Current?.Message;
    if (msg != null && msg.TryGetProperty("domain_id", out string domainIdStr) &&
        Guid.TryParse(domainIdStr, out var domainId))
    {
      return Task.FromResult(
          DomainResolutionResult.Succeeded(domainId, "Resolved domain from message property 'domain_id'.")
      );
    }

    return Task.FromResult(
        DomainResolutionResult.FailedResult("No valid domain_id property found on message.")
    );
  }
}
