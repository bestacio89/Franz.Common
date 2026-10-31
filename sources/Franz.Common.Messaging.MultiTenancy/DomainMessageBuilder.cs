#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy;

/// <summary>Propagates only a domain identity validated within the current tenant context.</summary>
public sealed class DomainMessageBuilder(ITenantExecutionContextAccessor executionContextAccessor) : IMessageBuilder
{
    public bool CanBuild(Message message) => executionContextAccessor.Current?.DomainId is not null;

    public Task BuildAsync(Message message, CancellationToken ct = default)
    {
        var domainId = executionContextAccessor.Current?.DomainId;
        if (domainId.HasValue)
            message.Headers[HeaderConstants.DomainId] = [domainId.Value.ToString()];
        return Task.CompletedTask;
    }
}
