using Franz.Common.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy;

public class TenantMessageBuilder : IMessageBuilder
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private readonly ITenantContextAccessor? tenantContextAccessor;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public TenantMessageBuilder(ITenantContextAccessor? tenantContextAccessor = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public bool CanBuild(Message message)
    {
        var result = tenantContextAccessor?.GetCurrentTenantId().HasValue == true;

        return result;
    }

    public void Build(Message message)
    {
        var id = tenantContextAccessor?.GetCurrentTenantId();

        if (id != null)
            message.Headers.Add(HeaderConstants.TenantId, id.ToString());
    }
}
