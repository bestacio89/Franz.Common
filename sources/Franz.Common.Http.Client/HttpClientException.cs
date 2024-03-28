using System.Net;

namespace Franz.Common.Http.Client;

public class HttpClientException : Exception
{
    public HttpStatusCode HttpStatusCode { get; }

    public HttpClientException()
    {
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public HttpClientException(string? message)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
      : base(message)
    {
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public HttpClientException(string? message, Exception innerException)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
      : base(message, innerException)
    {
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public HttpClientException(string? message, HttpStatusCode httpStatusCode)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
      : base(message)
    {
        HttpStatusCode = httpStatusCode;
    }
}
