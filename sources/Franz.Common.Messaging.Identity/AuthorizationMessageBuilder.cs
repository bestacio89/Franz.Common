using Franz.Common.Headers;

namespace Franz.Common.Messaging.Identity;

public class AuthorizationMessageBuilder : IMessageBuilder
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private readonly IHeaderContextAccessor? headerContextAccessor;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public AuthorizationMessageBuilder(IHeaderContextAccessor? headerContextAccessor = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        this.headerContextAccessor = headerContextAccessor;
    }

    public bool CanBuild(Message message)
    {
        var result = headerContextAccessor != null;

        return result;
    }

    public void Build(Message message)
    {
        AddAuthorization(message);
    }

    private void AddAuthorization(Message message)
    {
        if (headerContextAccessor!.TryGetValue(HeaderConstants.Authorization, out var result))
            message.Headers.Add(HeaderConstants.Authorization, result.ToString());
    }
}
