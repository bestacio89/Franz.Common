using Franz.Common.Messaging.Headers;

using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging;

public class Message
{
    public Message()
    {
        Headers = new MessageHeaders();
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Message(string? body)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
      : this()
    {
        Body = body;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Message(string? body, IEnumerable<KeyValuePair<string, StringValues>> headers)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        Body = body;
        Headers = new MessageHeaders(headers);
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Message(string? body, IEnumerable<KeyValuePair<string, string[]>> headers)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        Body = body;
        Headers = new MessageHeaders(headers.Select(header => new KeyValuePair<string, StringValues>(header.Key, new StringValues(header.Value))));
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public virtual string? Body { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    public virtual MessageHeaders Headers { get; set; }

  /// <summary>
  /// User-defined metadata for business-level properties that should not be mixed with transport headers.
  /// </summary>
  public virtual IDictionary<string, object> Properties { get; set; }
}
