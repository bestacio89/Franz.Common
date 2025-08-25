using System.Runtime.Serialization;

namespace Franz.Common.Errors;

[Serializable]
public class NotFoundException : ExceptionBase
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }


}
