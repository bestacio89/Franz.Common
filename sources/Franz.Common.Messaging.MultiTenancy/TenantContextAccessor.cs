using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy;

public class TenantContextAccessor : ITenantContextAccessor
{
    private readonly IMessageContextAccessor messageContextAccessor;

    public TenantContextAccessor(IMessageContextAccessor messageContextAccessor)
    {
        this.messageContextAccessor = messageContextAccessor;
    }

    public Guid? GetCurrentId()
    {
        Guid? result = null;

        if (messageContextAccessor.Current != null && messageContextAccessor.Current.Message.Headers.TryGetTenantId(out var tenantId))
            result = tenantId;

        return result;
    }
}
