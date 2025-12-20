using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

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
    if (headerContextAccessor is null)
      return;

    if (headerContextAccessor.TryGetValue(
          HeaderConstants.Authorization,
          out StringValues values))
    {
      message.Headers[HeaderConstants.Authorization] = values;
    }
  }
}
