using Franz.Common.Identity;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Headers;

namespace Franz.Common.Messaging.Identity;

public class IdentityContextAccessor : IIdentityContextAccessor
{
    private readonly IMessageContextAccessor messageContextAccessor;

    public IdentityContextAccessor(IMessageContextAccessor messageContextAccessor)
    {
        this.messageContextAccessor = messageContextAccessor;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public string? GetCurrentEmail()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        string? result = null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        if (messageContextAccessor.Current != null && messageContextAccessor.Current.Message.Headers.TryGetIdentityEmail(out var userEmail))
            result = userEmail;

        return result;
    }

    public Guid? GetCurrentId()
    {
        Guid? result = null;

        if (messageContextAccessor.Current != null && messageContextAccessor.Current.Message.Headers.TryGetIdentityId(out var userId))
            result = userId;

        return result;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public string? GetCurrentFullName()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        string? result = null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        if (messageContextAccessor.Current != null && messageContextAccessor.Current.Message.Headers.TryGetIdentityFullName(out var userFullName))
            result = userFullName;

        return result;
    }
}
