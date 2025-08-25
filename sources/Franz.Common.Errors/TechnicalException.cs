using System.Runtime.Serialization;

namespace Franz.Common.Errors;

[Serializable]
public class TechnicalException : ExceptionBase
{
    public TechnicalException()
    {
    }

    public TechnicalException(string message)
        : base(message)
    {
    }

    public TechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

 
}
