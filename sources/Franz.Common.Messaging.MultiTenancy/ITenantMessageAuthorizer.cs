using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.Messaging.MultiTenancy;

/// <summary>
/// Verifies that the authenticated transport producer is allowed to publish for the resolved tenant.
/// Implementations must use trusted broker/transport identity, not an unverified message header alone.
/// </summary>
public interface ITenantMessageAuthorizer
{
    Task<bool> IsAuthorizedAsync(TenantInfo tenant, Message message, CancellationToken cancellationToken = default);
}
