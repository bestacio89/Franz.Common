using Franz.Common.Errors;
using Newtonsoft.Json;

namespace Franz.Common.Http.Client.Delegating;

public class ExceptionDelegatingHandler : DelegatingHandler
{
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    var result = await base.SendAsync(request, cancellationToken);

    if (!result.IsSuccessStatusCode)
    {
      var message = await GetMessage(result, cancellationToken);
      throw new HttpClientException(message, result.StatusCode);
    }

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private async Task<string?> GetMessage(HttpResponseMessage httpResponseMessage, CancellationToken cancellationToken)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var json = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    string? result;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    try
    {
      var error = JsonConvert.DeserializeObject<ErrorResponse>(json);

      result = error?.Message ?? httpResponseMessage.ReasonPhrase;
    }
    catch
    {
      result = httpResponseMessage.ReasonPhrase;
    }

    return result;
  }
}
