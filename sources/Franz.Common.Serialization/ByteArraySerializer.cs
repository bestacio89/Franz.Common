using System.Text;

namespace Franz.Common.Serialization;

public class ByteArraySerializer : IByteArraySerializer
{
    private readonly IJsonSerializer jsonSerializer;

    public ByteArraySerializer(IJsonSerializer jsonSerializer)
    {
        this.jsonSerializer = jsonSerializer;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public byte[]? Serialize(object? content)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        var results = Array.Empty<byte>();

        if (content != null)
        {
            var json = jsonSerializer.Serialize(content);
            results = Encoding.UTF8.GetBytes(json!);
        }

        return results;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public TOut? Deserialize<TOut>(byte[]? content)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        TOut? result = default;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        if (content != null)
        {
            var objectString = Encoding.UTF8.GetString(content);
            result = jsonSerializer.Deserialize<TOut>(objectString);
        }

        return result;
    }
}
