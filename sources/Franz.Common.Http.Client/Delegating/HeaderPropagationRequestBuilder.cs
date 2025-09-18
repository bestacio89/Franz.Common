using Franz.Common.Extensions;
using Franz.Common.Headers;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Http.Client.Delegating;

public class HeaderPropagationRequestBuilder : IRequestBuilder
{
  private const int JustOne = 1;
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private readonly IHeaderContextAccessor? headerContextAccessor;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private readonly HeaderPropagationOptions? headerPropagationOptions;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private readonly IHeaderPropagationRegistrer? headerPropagationRegistrer;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public HeaderPropagationRequestBuilder(IHeaderContextAccessor? headerContextAccessor = null, IHeaderPropagationRegistrer? headerPropagationRegistrer = null, HeaderPropagationOptions? headerPropagationOptions = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    this.headerContextAccessor = headerContextAccessor;
    this.headerPropagationOptions = headerPropagationOptions;
    this.headerPropagationRegistrer = headerPropagationRegistrer;
  }

  public bool CanBuild(HttpRequestMessage request)
  {
    var result = headerContextAccessor is not null && (headerPropagationOptions?.Headers.Any() == true || headerPropagationRegistrer?.Headers.Any() == true);

    return result;
  }

  public void Build(HttpRequestMessage request)
  {
    headerPropagationOptions?.Headers
    .ForEach(header =>
    {
      Add(request, header);
    });

    headerPropagationRegistrer?.Headers
     .ForEach(header =>
     {
       Add(request, header.HeaderName);
     });
  }

  private void Add(HttpRequestMessage request, string header)
  {
    if (headerContextAccessor!.TryGetValue(header, out var stringValues))
    {
      if (HasMoreThanOneElement(stringValues))
        request.Headers.Add(header, stringValues.ToArray());
      else
        request.Headers.Add(header, stringValues.SingleOrDefault());
    }
  }

  private static bool HasMoreThanOneElement(StringValues stringValues)
  {
    var result = stringValues.Count > JustOne;

    return result;
  }
}
