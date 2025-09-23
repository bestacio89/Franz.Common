using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Headers;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging;

public class Message : INotification
{
  public Message() { }

  public Message(string? body)
  {
    Body = body;
  }

  public Message(string? body, IEnumerable<KeyValuePair<string, StringValues>> headers)
  {
    Body = body;
    Headers = new MessageHeaders(headers);
  }

  public Message(string? body, IEnumerable<KeyValuePair<string, string[]>> headers)
  {
    Body = body;
    Headers = new MessageHeaders(
        headers.Select(header =>
            new KeyValuePair<string, StringValues>(
                header.Key, new StringValues(header.Value)
            )
        )
    );
  }

  /// <summary>
  /// The raw message body (payload). Can be null if the transport didn't carry a body.
  /// </summary>
  public virtual string? Body { get; set; }

  /// <summary>
  /// Transport-level headers (Kafka, HTTP, etc.)
  /// Always initialized to avoid null checks.
  /// </summary>
  public virtual MessageHeaders Headers { get; set; } = new();

  /// <summary>
  /// User-defined metadata for business-level properties that should not be mixed with transport headers.
  /// Always initialized to a dictionary to avoid null checks.
  /// </summary>
  public virtual IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
