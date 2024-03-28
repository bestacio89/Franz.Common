using System.Runtime.Serialization;

namespace Franz.Common.Errors;

[Serializable]
public class ForbiddenException : Exception
{
    public ForbiddenException()
    {
    }

    public ForbiddenException(string message)
      : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

    protected ForbiddenException(SerializationInfo serializationInfo, StreamingContext streamingContext)
      : base(serializationInfo, streamingContext)
    {
    }
}
