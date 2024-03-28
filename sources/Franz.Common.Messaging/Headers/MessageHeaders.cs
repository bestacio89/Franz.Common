using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Headers;

public class MessageHeaders : Dictionary<string, StringValues>
{
    public MessageHeaders()
    {
    }

    public MessageHeaders(IEnumerable<KeyValuePair<string, StringValues>> values)
    {
        values
          .ToList()
          .ForEach(value => Add(value.Key, value.Value));
    }
}
