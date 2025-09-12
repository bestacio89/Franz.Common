using Franz.Common.Headers;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy;

public class DomainMessageBuilder : IMessageBuilder
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private readonly IDomainContextAccessor? domainContextAccessor;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public DomainMessageBuilder(IDomainContextAccessor? domainContextAccessor = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        this.domainContextAccessor = domainContextAccessor;
    }

    public bool CanBuild(Message message)
    {
        var result = domainContextAccessor?.GetCurrentDomainId().HasValue == true;

        return result;
    }

    public void Build(Message message)
    {
        var id = domainContextAccessor?.GetCurrentDomainId();

        if (id != null)
            message.Headers.Add(HeaderConstants.DomainId, id.ToString());
    }
}
