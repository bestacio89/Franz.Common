using System.Runtime.Serialization;

namespace Franz.Common.Errors;

[Serializable]
public class PreconditionFailedException : ExceptionBase
{
    public PreconditionFailedException()
    {
    }

    public PreconditionFailedException(string message)
        : base(message)
    {
    }

    public PreconditionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

   
}
