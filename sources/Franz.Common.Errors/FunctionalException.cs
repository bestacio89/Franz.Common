using System.Runtime.Serialization;

namespace Franz.Common.Errors;

[Serializable]
public class FunctionalException : ExceptionBase
{
    public FunctionalException()
    {
    }

    public FunctionalException(string message)
        : base(message)
    {
    }

    public FunctionalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

  
}
