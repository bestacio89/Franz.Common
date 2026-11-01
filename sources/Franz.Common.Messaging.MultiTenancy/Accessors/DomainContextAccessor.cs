#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.MultiTenancy;

namespace Franz.Common.Messaging.MultiTenancy.Accessors;

/// <summary>Reads validated domain identity and writes metadata only for outbound propagation.</summary>
public sealed class DomainContextAccessor(
    IMessageContextAccessor messageContextAccessor,
    ITenantExecutionContextAccessor executionContextAccessor) : IDomainContextAccessor
{
    public Guid? GetCurrentId() => GetCurrentDomainId();
    public void SetCurrentId(Guid domainId) => SetCurrentDomainId(domainId);

    public Guid? GetCurrentDomainId() => executionContextAccessor.Current?.DomainId;

    public void SetCurrentDomainId(Guid domainId)
    {
        var headers = messageContextAccessor.Current?.Message.Headers;
        if (headers is not null)
            headers[HeaderConstants.DomainId] = [domainId.ToString()];
    }
}
