using Franz.Common.Headers;

namespace Franz.Common.Messaging.Headers;

public class HeaderPropagationMessageBuilder : IMessageBuilder
{
    private readonly IHeaderContextAccessor headerContextAccessor;
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private readonly IHeaderPropagationRegistrer? headerPropagationRegistrer;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private readonly HeaderPropagationOptions? headerPropagationOptions;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public HeaderPropagationMessageBuilder(IHeaderContextAccessor headerContextAccessor, IHeaderPropagationRegistrer? headerPropagationRegistrer = null, HeaderPropagationOptions? headerPropagationOptions = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        this.headerContextAccessor = headerContextAccessor;
        this.headerPropagationRegistrer = headerPropagationRegistrer;
        this.headerPropagationOptions = headerPropagationOptions;
    }

    public bool CanBuild(Message message)
    {
        var result = headerPropagationOptions?.Headers.Any() == true || headerPropagationRegistrer?.Headers.Any() == true;

        return result;
    }

    public void Build(Message message)
    {
        headerPropagationOptions?.Headers
          .ToList()
          .ForEach(header =>
          {
              if (headerContextAccessor.TryGetValue(header, out var value))
                  message.Headers.Add(header, value);
          });

        headerPropagationRegistrer?.Headers
          .ToList()
          .ForEach(header =>
          {
              if (headerContextAccessor.TryGetValue(header.HeaderName, out var value))
                  message.Headers.Add(header.HeaderName, value);
          });
    }
}
