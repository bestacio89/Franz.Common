#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy;

/// <summary>Propagates only the validated tenant and domain context into outbound message metadata.</summary>
public sealed class TenantMessageBuilder(ITenantExecutionContextAccessor executionContextAccessor) : IMessageBuilder
{
    public bool CanBuild(Message message) => executionContextAccessor.Current is not null;

    public Task BuildAsync(Message message, CancellationToken ct = default)
    {
        var context = executionContextAccessor.Current;
        if (context is null)
            return Task.CompletedTask;

        message.Headers[HeaderConstants.TenantId] = [context.TenantId.ToString()];

        if (context.DomainId is Guid domainId)
            message.Headers[HeaderConstants.DomainId] = [domainId.ToString()];
        else
            message.Headers.Remove(HeaderConstants.DomainId);

        return Task.CompletedTask;
    }
}
