using Franz.Common.Http.Client.Content;
using Franz.Common.Http.Client.Files;
using Franz.Common.IO;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web;

namespace Franz.Common.Http.Client;

public abstract class HttpService : IDisposable
{
  private readonly HttpClient httpClient;
  private readonly IEnumerable<JsonConverter> jsonConverters;
  private bool disposedValue;

  protected HttpService(IHttpClientFactory httpClientFactory, IEnumerable<JsonConverter> jsonConverters)
  {
    var assembly = GetType().Assembly;
    var namespaceName = string.Join(".", assembly.GetName().Name!.Split(".").Reverse().Skip(1).Reverse());

    httpClient = httpClientFactory.CreateClient(namespaceName);
    this.jsonConverters = jsonConverters ?? new List<JsonConverter>();
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected void GetRequest(string request, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    SendAsync(HttpMethod.Get, request, CancellationToken.None, null, headers).Wait();
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task GetRequestAsync(string request, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    await SendAsync(HttpMethod.Get, request, cancellationToken, null, headers);
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected TOut GetRequest<TOut>(string request, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = SendAsync<TOut>(HttpMethod.Get, request, CancellationToken.None, null, headers).Result;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task<TOut> GetRequestAsync<TOut>(string request, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = await SendAsync<TOut>(HttpMethod.Get, request, cancellationToken, null, headers);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected Stream DownloadRequest(string request, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    Stream result;

    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, request);

    AddHeaders(httpRequestMessage, headers);

    using var response = httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None).Result;
    response.EnsureSuccessStatusCode();

    using var streamToReadFrom = response.Content.ReadAsStream();
    result = new DeleteTemporaryFileAfterReadingStream();
    streamToReadFrom.CopyTo(result);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task<Stream> DownloadRequestAsync(string request, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    Stream result;

    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, request);

    AddHeaders(httpRequestMessage, headers);

    using var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    response.EnsureSuccessStatusCode();

    using var streamToReadFrom = response.Content.ReadAsStream();
    result = new DeleteTemporaryFileAfterReadingStream();
    streamToReadFrom.CopyTo(result);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected void PostRequest(string request, object? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = SerializeToStringContent(content);
    SendAsync(HttpMethod.Post, request, CancellationToken.None, httpContent, headers).Wait();
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task PostRequestAsync(string request, CancellationToken cancellationToken, object? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = SerializeToStringContent(content);
    await SendAsync(HttpMethod.Post, request, cancellationToken, httpContent, headers);
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected TOut PostRequest<TOut>(string request, object? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = SerializeToStringContent(content);
    var result = SendAsync<TOut>(HttpMethod.Post, request, CancellationToken.None, httpContent, headers).Result;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task<TOut> PostRequestAsync<TOut>(string request, CancellationToken cancellationToken, object? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = SerializeToStringContent(content);
    var result = await SendAsync<TOut>(HttpMethod.Post, request, cancellationToken, httpContent, headers);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected TOut PostRequest<TOut>(string request, IEnumerable<HttpContentParameter> httpContentParameters, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = GetMultipartFormDataContent(httpContentParameters);
    var result = SendAsync<TOut>(HttpMethod.Post, request, CancellationToken.None, httpContent, headers).Result;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task<TOut> PostRequestAsync<TOut>(string request, IEnumerable<HttpContentParameter> httpContentParameters, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = GetMultipartFormDataContent(httpContentParameters);
    var result = await SendAsync<TOut>(HttpMethod.Post, request, cancellationToken, httpContent, headers);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected void PostRequest(string request, IEnumerable<HttpContentParameter> httpContentParameters, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = GetMultipartFormDataContent(httpContentParameters);
    SendAsync(HttpMethod.Post, request, CancellationToken.None, httpContent, headers).Wait();
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task PostRequestAsync(string request, IEnumerable<HttpContentParameter> httpContentParameters, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = GetMultipartFormDataContent(httpContentParameters);
    await SendAsync(HttpMethod.Post, request, cancellationToken, httpContent, headers);
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected void PutRequest(string request, object? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = SerializeToStringContent(content);
    SendAsync(HttpMethod.Put, request, CancellationToken.None, httpContent, headers).Wait();
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task PutRequestAsync(string request, CancellationToken cancellationToken, object? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = SerializeToStringContent(content);
    await SendAsync(HttpMethod.Put, request, cancellationToken, httpContent, headers);
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected TOut PutRequest<TOut>(string request, object? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = SerializeToStringContent(content);
    var result = SendAsync<TOut>(HttpMethod.Put, request, CancellationToken.None, httpContent, headers).Result;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task<TOut> PutRequestAsync<TOut>(string request, CancellationToken cancellationToken, object? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = SerializeToStringContent(content);
    var result = await SendAsync<TOut>(HttpMethod.Put, request, cancellationToken, httpContent, headers);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected TOut PutRequest<TOut>(string request, IEnumerable<HttpContentParameter> httpContentParameters, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = GetMultipartFormDataContent(httpContentParameters);
    var result = SendAsync<TOut>(HttpMethod.Put, request, CancellationToken.None, httpContent, headers).Result;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task<TOut> PutRequestAsync<TOut>(string request, IEnumerable<HttpContentParameter> httpContentParameters, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = GetMultipartFormDataContent(httpContentParameters);
    var result = await SendAsync<TOut>(HttpMethod.Put, request, cancellationToken, httpContent, headers);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected void PutRequest(string request, IEnumerable<HttpContentParameter> httpContentParameters, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = GetMultipartFormDataContent(httpContentParameters);
    SendAsync(HttpMethod.Put, request, cancellationToken, httpContent, headers).Wait();
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task PutRequestAsync(string request, IEnumerable<HttpContentParameter> httpContentParameters, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpContent = GetMultipartFormDataContent(httpContentParameters);
    await SendAsync(HttpMethod.Put, request, cancellationToken, httpContent, headers);
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected void DeleteRequest(string request, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    SendAsync(HttpMethod.Delete, request, CancellationToken.None, null, headers).Wait();
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task DeleteRequestAsync(string request, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    await SendAsync(HttpMethod.Delete, request, cancellationToken, null, headers);
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected TOut DeleteRequest<TOut>(string request, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = SendAsync<TOut>(HttpMethod.Delete, request, CancellationToken.None, null, headers).Result;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  protected async Task<TOut> DeleteRequestAsync<TOut>(string request, CancellationToken cancellationToken, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = await SendAsync<TOut>(HttpMethod.Delete, request, cancellationToken, null, headers);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  internal StringContent SerializeToStringContent(object? content)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var json = Serialize(content);

    var result = new StringContent(json, Encoding.UTF8, "application/json");

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private async Task SendAsync(HttpMethod method, string request, CancellationToken cancellationToken, HttpContent? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpRequestMessage = new HttpRequestMessage(method, request);

    AddHeaders(httpRequestMessage, headers);

    httpRequestMessage.Content = content;

    await httpClient.SendAsync(httpRequestMessage, cancellationToken);
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private async Task<TOut> SendAsync<TOut>(HttpMethod method, string request, CancellationToken cancellationToken, HttpContent? content = null, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    using var httpRequestMessage = new HttpRequestMessage(method, request);

    AddHeaders(httpRequestMessage, headers);

    httpRequestMessage.Content = content;

    using var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
    var result = ExtractResult<TOut>(response);

#pragma warning disable CS8603 // Possible null reference return.
    return result;
#pragma warning restore CS8603 // Possible null reference return.
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private string Serialize(object? content)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = content != null ? content is string stringContent ? stringContent : JsonConvert.SerializeObject(content, jsonConverters.ToArray()) : string.Empty;

    return result;
  }

  [return: MaybeNull]
  internal TOut ExtractResult<TOut>(HttpResponseMessage response)
  {
    TOut result;
    string responseContent;

    response.EnsureSuccessStatusCode();
    responseContent = response.Content.ReadAsStringAsync().Result;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    result = typeof(TOut) != typeof(string) ? JsonConvert.DeserializeObject<TOut>(responseContent, jsonConverters.ToArray()) : (TOut)(object)responseContent;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

    return result;
  }

  internal static MultipartFormDataContent GetMultipartFormDataContent(IEnumerable<HttpContentParameter> httpContentParameters)
  {
    var multipartFormDataContent = new MultipartFormDataContent();

    foreach (var httpContentParameter in httpContentParameters)
      httpContentParameter.AddTo(multipartFormDataContent);

    return multipartFormDataContent;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public virtual string GenerateListQueryParameter(string uri, string parameterName, IEnumerable values, string? format = null, object? defaultValue = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = uri;
    var i = 0;

    if (values != null)
    {
      foreach (var value in values)
        result = GenerateQueryParameter(result, $"{parameterName}[{i++}]", value, format, defaultValue);
    }

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public virtual string GenerateQueryParameter(string uri, string parameterName, object? parameter, string? format = null, object? defaultValue = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = uri;

    if (parameter != defaultValue && parameter != null)
    {
      var separator = !uri.Contains("?") ? "?" : "&";

      result += $"{separator}{parameterName}={GenerateValueParameter(parameter, format)}";
    }

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public virtual string GenerateValueParameter(object value, string? format = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = string.Empty;

    if (value is not null)
    {
      result = value is IFormattable formattable && !string.IsNullOrEmpty(format)
        ? HttpUtility.UrlEncode(formattable.ToString(format, Thread.CurrentThread.CurrentUICulture))
        : IsDateTimeOffset(value) ? HttpUtility.UrlEncode(value.ToString()) : HttpUtility.UrlPathEncode(value.ToString());
    }

#pragma warning disable CS8603 // Possible null reference return.
    return result;
#pragma warning restore CS8603 // Possible null reference return.
  }

  private static bool IsDateTimeOffset(object value)
  {
    var result = value is DateTimeOffset || value is DateTimeOffset?;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public virtual ICollection<HttpContentParameter> GenerateHttpContentParameters(string parameterName, object content, ICollection<HttpContentParameter>? httpContentParameters = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    httpContentParameters ??= new Collection<HttpContentParameter>();

    var jsonContent = SerializeToStringContent(content);

    var httpContentParameter = new HttpContentParameter(parameterName, jsonContent);
    httpContentParameters.Add(httpContentParameter);

    return httpContentParameters;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public virtual ICollection<HttpContentParameter> GenerateHttpContentParameters(string parameterName, FileParameter fileParameter, ICollection<HttpContentParameter>? httpContentParameters = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    httpContentParameters ??= new Collection<HttpContentParameter>();

    if (fileParameter?.Stream?.Length > 0)
    {
      if (fileParameter.Stream.CanSeek)
        fileParameter.Stream.Seek(0, SeekOrigin.Begin);

      var streamContent = new StreamContent(fileParameter.Stream);
      streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fileParameter.MimeType);

      var httpFileParameter = new HttpFileParameter(parameterName, fileParameter.Name, streamContent);
      httpContentParameters.Add(httpFileParameter);
    }

    return httpContentParameters;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public virtual ICollection<HttpContentParameter>? GenerateHttpContentParameters(string parameterName, IEnumerable<FileParameter> fileParameters, ICollection<HttpContentParameter>? httpContentParameters = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    foreach (var fileParameter in fileParameters)
      httpContentParameters = GenerateHttpContentParameters(parameterName, fileParameter, httpContentParameters);

    return httpContentParameters;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public virtual void AddHeaders(HttpRequestMessage httpRequestMessage, IDictionary<string, object>? headers = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    headers?.ToList().ForEach(header =>
    {
      AddHeader(httpRequestMessage, header);
    });
  }

  public virtual void AddHeader(HttpRequestMessage httpRequestMessage, KeyValuePair<string, object> header)
  {
    var value = Serialize(header.Value);

    httpRequestMessage.Headers.Add(header.Key, value);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposedValue)
    {
      if (disposing)
        httpClient.Dispose();

      disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
