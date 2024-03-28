namespace Franz.Common.Http.Client.Content;

public class HttpContentParameter
{
    public string ParameterName { get; set; }

    public HttpContent HttpContent { get; set; }

    public HttpContentParameter(string parameterName, HttpContent httpContent)
    {
        ParameterName = parameterName;
        HttpContent = httpContent;
    }

    internal virtual void AddTo(MultipartFormDataContent multipartFormDataContent)
    {
        multipartFormDataContent.Add(HttpContent, ParameterName);
    }
}
