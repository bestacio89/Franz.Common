using System.Runtime.Serialization;

namespace Franz.Common.Errors;

[Serializable]
public abstract class ExceptionBase : Exception
{
    protected ExceptionBase()
    {
    }

    protected ExceptionBase(string message)
        : base(message)
    {
    }

    protected ExceptionBase(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected ExceptionBase(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {
    }
}
