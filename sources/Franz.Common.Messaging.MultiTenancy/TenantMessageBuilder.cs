#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy;

/// <summary>Propagates only the validated tenant context into outbound message metadata.</summary>
public sealed class TenantMessageBuilder(ITenantExecutionContextAccessor executionContextAccessor) : IMessageBuilder
{
    public bool CanBuild(Message message) => executionContextAccessor.Current is not null;

    public Task BuildAsync(Message message, CancellationToken ct = default)
    {
        var tenantId = executionContextAccessor.Current?.TenantId;
        if (tenantId.HasValue)
            message.Headers[HeaderConstants.TenantId] = [tenantId.Value.ToString()];
        return Task.CompletedTask;
    }
}
