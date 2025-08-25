using System.Runtime.Serialization;

namespace Franz.Common.Errors;

[Serializable]
public class UnauthorizedException : ExceptionBase
{
    public UnauthorizedException()
    {
    }

    public UnauthorizedException(string message)
        : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    
}
