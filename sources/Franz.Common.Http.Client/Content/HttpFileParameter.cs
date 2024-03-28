namespace Franz.Common.Http.Client.Content;

public class HttpFileParameter : HttpContentParameter
{
    public string FileName { get; }

    public HttpFileParameter(string parameterName, string fileName, StreamContent streamContent)
      : base(parameterName, streamContent)
    {
        FileName = fileName;
    }

    internal override void AddTo(MultipartFormDataContent multipartFormDataContent)
    {
        multipartFormDataContent.Add(HttpContent, ParameterName, FileName);
    }
}
