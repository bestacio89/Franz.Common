using Franz.Common.Extensions;

namespace Franz.Common.Http.Client.Delegating;

public class RequestBuilderDelegatingHandler : DelegatingHandler
{
  private readonly IEnumerable<IRequestBuilder> requestBuilders;

  public RequestBuilderDelegatingHandler(IEnumerable<IRequestBuilder> requestBuilders)
  {
    this.requestBuilders = requestBuilders;
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    requestBuilders
      .Where(requestBuilder => requestBuilder.CanBuild(request))
      .ForEach(requestBuilder => requestBuilder.Build(request));

    var result = await base.SendAsync(request, cancellationToken);

    return result;
  }
}
